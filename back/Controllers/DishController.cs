using back.Data;
using back.Enums;
using back.Helpers;
using back.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace back.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DishController : ControllerBase
{
    private readonly MyDbContext _context;
    private readonly IWebHostEnvironment _env;

    public DishController(MyDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    private async Task<List<string>> SaveImagesAsync(List<IFormFile> images)
    {
        var urls = new List<string>();
        var folder = Path.Combine(_env.WebRootPath, "uploads", "dishes");
        Directory.CreateDirectory(folder);

        foreach (var img in images)
        {
            if (img.Length == 0) continue;
            var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
            var filePath = Path.Combine(folder, uniqueName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await img.CopyToAsync(stream);

            urls.Add($"/uploads/dishes/{uniqueName}");
        }
        return urls;
    }

    [HttpPost]
    public async Task<ActionResult> CreateDish([FromForm] DishRequestDto request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (name, categoryFromMacro) = ParseMacros(request.Name);

        var imageUrls = new List<string>();
        if (request.Images != null && request.Images.Any())
        {
            imageUrls = await SaveImagesAsync(request.Images);
        }

        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = name,
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            Carbohydrates = 0,
            PortionSize = request.PortionSize,
            Category = request.Category ?? categoryFromMacro ?? DishCategory.Second,
            ImageUrl = imageUrls,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var ing in request.Ingredients)
        {
            var product = _context.Products.Find(ing.ProductId);
            if (product == null)
                return BadRequest($"Продукт с id {ing.ProductId} не найден");

            dish.DishesProducts.Add(new DishesProduct
            {
                Id = Guid.NewGuid(),
                DishId = dish.Id,
                ProductId = ing.ProductId,
                Product = product, 
                Amount = ing.Amount
            });
        }

        var (calories, proteins, fats, carbs) = DishCalculator.Calculate(dish);
        dish.Calories = request.Calories ?? calories;
        dish.Proteins = request.Proteins ?? proteins;
        dish.Fats = request.Fats ?? fats;
        dish.Carbohydrates = request.Carbohydrates ?? carbs;

        dish.DietaryFlags = GetAvailableFlags(dish);

        if (request.DietaryFlags.HasValue)
            dish.DietaryFlags &= request.DietaryFlags.Value;

        var bjuPer100g = CalculateBjuPer100g(dish);
        if (bjuPer100g.proteins + bjuPer100g.fats + bjuPer100g.carbs > 100)
            return BadRequest("Сумма БЖУ на 100г не может превышать 100г");

        _context.Dishes.Add(dish);
        _context.SaveChanges();

        return CreatedAtAction(nameof(GetDishById), new { id = dish.Id }, dish);
    }

    [HttpGet]
    public ActionResult GetDishList(
        [FromQuery] DishCategory? category,
        [FromQuery] DietaryFlags? dietaryFlags,
        [FromQuery] string? search,
        [FromQuery] string? sortBy = "name",
        [FromQuery] bool ascending = true)
    {
        var query = _context.Dishes
            .Include(d => d.DishesProducts)
            .ThenInclude(dp => dp.Product)
            .AsQueryable();

        if (category.HasValue)
            query = query.Where(d => d.Category == category);

        if (dietaryFlags.HasValue && dietaryFlags != DietaryFlags.None)
            query = query.Where(d => (d.DietaryFlags & dietaryFlags) == dietaryFlags);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => EF.Functions.ILike(d.Name, $"%{search}%"));

        query = sortBy?.ToLower() switch
        {
            "name" => ascending ? query.OrderBy(d => d.Name) : query.OrderByDescending(d => d.Name),
            "calories" => ascending ? query.OrderBy(d => d.Calories) : query.OrderByDescending(d => d.Calories),
            "proteins" => ascending ? query.OrderBy(d => d.Proteins) : query.OrderByDescending(d => d.Proteins),
            "fats" => ascending ? query.OrderBy(d => d.Fats) : query.OrderByDescending(d => d.Fats),
            "carbs" => ascending ? query.OrderBy(d => d.Carbohydrates) : query.OrderByDescending(d => d.Carbohydrates),
            _ => ascending ? query.OrderBy(d => d.Name) : query.OrderByDescending(d => d.Name)
        };

        return Ok(query.ToList());
    }

    [HttpGet("{id}")]
    public ActionResult GetDishById(Guid id)
    {
        var dish = _context.Dishes
            .Include(d => d.DishesProducts)
            .ThenInclude(dp => dp.Product)
            .FirstOrDefault(d => d.Id == id);

        if (dish == null)
            return NotFound($"Блюдо с id {id} не найдено");

        return Ok(dish);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> UpdateDish(Guid id, [FromForm] DishRequestDto request)
    {
        var dish = _context.Dishes
            .Include(d => d.DishesProducts)
            .FirstOrDefault(d => d.Id == id);

        if (dish == null)
            return NotFound();

        var (name, categoryFromMacro) = ParseMacros(request.Name);
        dish.Name = request.Name != null ? name : dish.Name;
        dish.PortionSize = request.PortionSize != 0 ? request.PortionSize : dish.PortionSize;
        dish.Category = request.Category ?? categoryFromMacro ?? dish.Category;

        if (request.Images != null && request.Images.Any())
        {
            var imageUrls = await SaveImagesAsync(request.Images);
            dish.ImageUrl = imageUrls;
        }
        else if (request.ImageUrl != null)
        {
            dish.ImageUrl = request.ImageUrl;
        }

        if (request.Ingredients != null && request.Ingredients.Any())
        {
            var oldIngredients = dish.DishesProducts.ToList();
            _context.DishesProducts.RemoveRange(oldIngredients);
            _context.SaveChanges();

            dish.DishesProducts.Clear();

            foreach (var ing in request.Ingredients)
            {
                var product = _context.Products.Find(ing.ProductId);
                if (product == null)
                    return BadRequest($"Продукт с id {ing.ProductId} не найден");

                _context.DishesProducts.Add(new DishesProduct
                {
                    Id = Guid.NewGuid(),
                    DishId = dish.Id,
                    ProductId = ing.ProductId,
                    Product = product,
                    Amount = ing.Amount
                });
            }
            _context.SaveChanges();
        }

        var ingredients = _context.DishesProducts
            .Where(dp => dp.DishId == dish.Id)
            .Include(dp => dp.Product)
            .ToList();

        dish.DishesProducts = ingredients;

        var (calories, proteins, fats, carbs) = DishCalculator.Calculate(dish);

        dish.Calories = request.Calories ?? calories;
        dish.Proteins = request.Proteins ?? proteins;
        dish.Fats = request.Fats ?? fats;
        dish.Carbohydrates = request.Carbohydrates ?? carbs;

        var availableFlags = GetAvailableFlags(dish);
        dish.DietaryFlags = request.DietaryFlags ?? dish.DietaryFlags;
        dish.DietaryFlags &= availableFlags;

        var bjuPer100g = CalculateBjuPer100g(dish);
        if (bjuPer100g.proteins + bjuPer100g.fats + bjuPer100g.carbs > 100)
            return BadRequest("Сумма БЖУ на 100г не может превышать 100г");

        dish.UpdatedAt = DateTime.UtcNow;
        _context.SaveChanges();

        var result = _context.Dishes
            .Include(d => d.DishesProducts)
            .ThenInclude(dp => dp.Product)
            .FirstOrDefault(d => d.Id == dish.Id);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public ActionResult DeleteDish(Guid id)
    {
        var dish = _context.Dishes.Find(id);
        if (dish == null)
            return NotFound($"Блюдо с id {id} не найдено");

        _context.Dishes.Remove(dish);
        _context.SaveChanges();

        return Ok(dish);
    }

    private (string name, DishCategory? category) ParseMacros(string name)
    {
        var macrosMap = new Dictionary<string, DishCategory>
        {
            ["!десерт"] = DishCategory.Dessert,
            ["!первое"] = DishCategory.First,
            ["!второе"] = DishCategory.Second,
            ["!напиток"] = DishCategory.Drink,
            ["!салат"] = DishCategory.Salad,
            ["!суп"] = DishCategory.Soup,
            ["!перекус"] = DishCategory.Snack
        };

        foreach (var macro in macrosMap)
        {
            if (name.Contains(macro.Key))
            {
                return (name.Replace(macro.Key, "").Trim(), macro.Value);
            }
        }

        return (name, null);
    }

    private DietaryFlags GetAvailableFlags(Dish dish)
    {
        if (!dish.DishesProducts.Any())
            return DietaryFlags.None;
            
        DietaryFlags available = DietaryFlags.Vegan | DietaryFlags.GlutenFree | DietaryFlags.SugarFree;

        foreach (var product in dish.DishesProducts)
        {
            if (product.Product == null) continue;
            available &= product.Product.DietaryFlags;
        }

        return available;
    }

    private (double proteins, double fats, double carbs) CalculateBjuPer100g(Dish dish)
    {
        if (dish.PortionSize <= 0)
            return (0, 0, 0);

        var factor = 100 / dish.PortionSize;
        return (
            dish.Proteins * factor,
            dish.Fats * factor,
            dish.Carbohydrates * factor
        );
    }
}