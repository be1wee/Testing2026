using back.Data;
using back.Enums;
using back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly IWebHostEnvironment _env;
    public ProductController(MyDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private async Task<List<string>> SaveImagesAsync(List<IFormFile> images)
    {
        var urls = new List<string>();
        var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);   

        foreach (var img in images)
        {
            if (img.Length == 0) continue;
            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
            var filePath = Path.Combine(folder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await img.CopyToAsync(stream);     

            urls.Add($"/uploads/products/{uniqueName}");
        }
        return urls;   
    }
    [HttpPost]
    public async Task<ActionResult> CreateProduct([FromForm] ProductRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var imageUrls = request.Images != null ? await SaveImagesAsync(request.Images) : new List<string>();

        var newProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Calories = request.Calories,
            Proteins = request.Proteins,
            Fats = request.Fats,
            Carbohydrates = request.Carbohydrates,
            Composition = request.Composition,
            Category = request.Category,
            Readiness = request.Readiness,
            DietaryFlags = request.DietaryFlags,
            ImageUrl = imageUrls,        
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null
        };

        await _context.Products.AddAsync(newProduct);
        await _context.SaveChangesAsync();

        return Ok(newProduct);
    }

    [HttpGet]
    public async Task<ActionResult> GetProductList()
    {
        var products = await _context.Products.ToListAsync();
        return Ok(products);
    }

    [HttpGet("filter")]
    public async Task<ActionResult> GetDetailedProductList(
        [FromQuery] ProductCategory? category,
        [FromQuery] Readiness? readiness,
        [FromQuery] DietaryFlags? dietaryFlags,
        [FromQuery] string? search,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool ascending = true)
    {
        var query = _context.Products.AsQueryable();

        if (category.HasValue)
            query = query.Where(p => p.Category == category);
        if (readiness.HasValue)
            query = query.Where(p => p.Readiness == readiness);
        if (dietaryFlags.HasValue && dietaryFlags != DietaryFlags.None)
            query = query.Where(p => (p.DietaryFlags & dietaryFlags) == dietaryFlags);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));

        query = sortBy?.ToLower() switch
        {
            "calories" => ascending ? query.OrderBy(p => p.Calories) : query.OrderByDescending(p => p.Calories),
            "proteins" => ascending ? query.OrderBy(p => p.Proteins) : query.OrderByDescending(p => p.Proteins),
            "fats" => ascending ? query.OrderBy(p => p.Fats) : query.OrderByDescending(p => p.Fats),
            "carbohydrates" => ascending ? query.OrderBy(p => p.Carbohydrates) : query.OrderByDescending(p => p.Carbohydrates),
            _ => ascending ? query.OrderBy(p => p.Name) : query.OrderByDescending(p => p.Name)
        };

        var products = await query.ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> GetProductById(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound($"Продукт с id {id} не найден");

        return Ok(product);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProductById(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.DishesProducts)
            .ThenInclude(dp => dp.Dish)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound($"Продукт с id {id} не найден");

        if (product.DishesProducts != null && product.DishesProducts.Any())
        {
            var dishesNames = product.DishesProducts
                .Select(dp => dp.Dish?.Name)
                .Where(name => name != null)
                .ToList();

            return BadRequest(new
            {
                Message = "Невозможно удалить продукт, так как он используется в блюдах",
                Dishes = dishesNames
            });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return Ok(product);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateProduct(Guid id, [FromForm] ProductRequestDto request)
    {
        Console.WriteLine("=== UpdateProduct called ===");
        var product = await _context.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        if (request.Name != null)
            product.Name = request.Name;
        product.Calories = request.Calories;
        product.Proteins = request.Proteins;
        product.Fats = request.Fats;
        product.Carbohydrates = request.Carbohydrates;
        if (request.Composition != null)
            product.Composition = request.Composition;
        if (request.Category != default)
            product.Category = request.Category;
        if (request.Readiness != default)
            product.Readiness = request.Readiness;
        product.DietaryFlags = request.DietaryFlags;
        if (request.Images != null && request.Images.Any())
        {
            product.ImageUrl = await SaveImagesAsync(request.Images);
        }

        product.UpdatedAt = DateTime.UtcNow;

        if (!TryValidateModel(product))
            return ValidationProblem();

        await _context.SaveChangesAsync();
        return Ok(product);
    }
}