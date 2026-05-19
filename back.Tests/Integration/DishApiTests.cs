using System.Net;
using System.Net.Http.Json;
using back.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace back.Tests.Integration;

public class DishApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public DishApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<Guid> CreateProductAsync(string name = "Продукт", string dietaryFlags = "0")
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent("200"), "Calories");
        form.Add(new StringContent("10"), "Proteins");
        form.Add(new StringContent("5"), "Fats");
        form.Add(new StringContent("20"), "Carbohydrates");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent("0"), "Readiness");
        form.Add(new StringContent(dietaryFlags), "DietaryFlags");

        var response = await _client.PostAsync("/api/product", form);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        return product!.Id;
    }


    [Fact]
    public async Task CreateDish_ValidData_ReturnsCreated()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Тестовое блюдо"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();
        Assert.NotNull(dish);
        Assert.Equal("Тестовое блюдо", dish.Name);
    }

    [Fact]
    public async Task CreateDish_WithMacros_SetsCategoryAndRemovesFromName()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("!супБорщ"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();
        Assert.Equal("Борщ", dish!.Name);
        Assert.Equal(5, (int)dish.Category); 
    }

    [Theory]
    [InlineData("Аб", HttpStatusCode.Created)]
    [InlineData("А", HttpStatusCode.BadRequest)]
    public async Task CreateDish_NameLength_ReturnsExpectedStatus(string name, HttpStatusCode expectedStatus)
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("200", HttpStatusCode.Created)]
    [InlineData("0", HttpStatusCode.BadRequest)]
    public async Task CreateDish_PortionSizeBoundary_ReturnsExpectedStatus(string portionSize, HttpStatusCode expectedStatus)
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Тест"), "Name");
        form.Add(new StringContent(portionSize), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task CreateDish_NoIngredients_ReturnsBadRequest()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Пустое блюдо"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDish_TwoIngredients_SumsCorrectly()
    {
        var product1Id = await CreateProductAsync("Первый");
        var product2Id = await CreateProductAsync("Второй");

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Два продукта"), "Name");
        form.Add(new StringContent("100"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{product1Id}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("100"), "Ingredients[0].Amount");
        form.Add(new StringContent($"{product2Id}"), "Ingredients[1].ProductId");
        form.Add(new StringContent("50"), "Ingredients[1].Amount");

        var response = await _client.PostAsync("/api/dish", form);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(300, dish!.Calories);
    }

    [Fact]
    public async Task CreateDish_NonExistingProduct_ReturnsBadRequest()
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Несуществующий продукт"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{Guid.NewGuid()}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("100"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateDish_AutoCalculatesCalories()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Блюдо"), "Name");
        form.Add(new StringContent("100"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();

        Assert.Equal(300, dish!.Calories); 
    }

    [Fact]
    public async Task CreateDish_ManualCalories_OverridesAutoCalculation()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Блюдо"), "Name");
        form.Add(new StringContent("100"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent("500"), "Calories");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();

        Assert.Equal(500, dish!.Calories);
    }

    [Theory]
    [InlineData("1", 1)]  // флаг Vegan установлен
    [InlineData("0", 0)]  // флаг Vegan сброшен
    public async Task CreateDish_VeganFlag_DependsOnProducts(string productFlags, int expectedFlags)
    {
        var productId = await CreateProductAsync("Тест", productFlags);

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Блюдо"), "Name");
        form.Add(new StringContent("100"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent("1"), "DietaryFlags");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("100"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);
        var dish = await response.Content.ReadFromJsonAsync<Dish>();

        Assert.Equal(expectedFlags, (int)dish!.DietaryFlags);
    }

    [Theory]
    [InlineData("10", "5", "20", HttpStatusCode.Created)]      // сумма 35 < 100
    [InlineData("40", "30", "30", HttpStatusCode.Created)]      // сумма = 100
    [InlineData("50", "50", "50", HttpStatusCode.BadRequest)]   // сумма 150 > 100
    public async Task CreateDish_NutrientSumBoundaries_ReturnsExpectedStatus(
        string proteins, string fats, string carbs, HttpStatusCode expectedStatus)
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Границы БЖУ"), "Name");
        form.Add(new StringContent("100"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent(proteins), "Proteins");
        form.Add(new StringContent(fats), "Fats");
        form.Add(new StringContent(carbs), "Carbohydrates");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("100"), "Ingredients[0].Amount");

        var response = await _client.PostAsync("/api/dish", form);

        Assert.Equal(expectedStatus, response.StatusCode);
    }



    [Fact]
    public async Task GetDishById_ExistingId_ReturnsDish()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Для получения"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var createResponse = await _client.PostAsync("/api/dish", form);
        var created = await createResponse.Content.ReadFromJsonAsync<Dish>();

        var response = await _client.GetAsync($"/api/dish/{created!.Id}");
        var dish = await response.Content.ReadFromJsonAsync<Dish>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(created.Id, dish!.Id);
    }

    [Fact]
    public async Task GetDishById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/dish/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }



    [Fact]
    public async Task DeleteDish_ExistingId_ReturnsOk()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Для удаления"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var createResponse = await _client.PostAsync("/api/dish", form);
        var created = await createResponse.Content.ReadFromJsonAsync<Dish>();

        var deleteResponse = await _client.DeleteAsync($"/api/dish/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/dish/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteDish_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/dish/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }



    [Theory]
    [InlineData("После обновления", "300", "10", "5", "20", HttpStatusCode.OK)]           // обычное
    [InlineData("Аб", "200", "5", "5", "10", HttpStatusCode.OK)]                           // имя 2 символа
    [InlineData("А", "200", "5", "5", "10", HttpStatusCode.BadRequest)]                    // имя 1 символ
    [InlineData("Без КБЖУ", "100", "0", "0", "0", HttpStatusCode.OK)]                      // нулевые КБЖУ
    [InlineData("Сумма 100", "100", "40", "30", "30", HttpStatusCode.OK)]                  // граница 100
    [InlineData("Сумма >100", "100", "50", "50", "50", HttpStatusCode.BadRequest)]         // превышение
    [InlineData("Отриц белки", "100", "-1", "0", "0", HttpStatusCode.BadRequest)]          // отриц белки
    [InlineData("Отриц жиры", "100", "0", "-1", "0", HttpStatusCode.BadRequest)]           // отриц жиры
    [InlineData("Отриц углев", "100", "0", "0", "-1", HttpStatusCode.BadRequest)]          // отриц углеводы
    public async Task UpdateDish_Boundaries_ReturnsExpectedStatus(
        string name, string portionSize, string proteins, string fats, string carbs, HttpStatusCode expectedStatus)
    {
        var productId = await CreateProductAsync();

        var createForm = new MultipartFormDataContent();
        createForm.Add(new StringContent("До обновления"), "Name");
        createForm.Add(new StringContent("200"), "PortionSize");
        createForm.Add(new StringContent("0"), "Category");
        createForm.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        createForm.Add(new StringContent("100"), "Ingredients[0].Amount");

        var createResponse = await _client.PostAsync("/api/dish", createForm);
        var created = await createResponse.Content.ReadFromJsonAsync<Dish>();

        var updateForm = new MultipartFormDataContent();
        updateForm.Add(new StringContent(name), "Name");
        updateForm.Add(new StringContent(portionSize), "PortionSize");
        updateForm.Add(new StringContent("0"), "Category");
        updateForm.Add(new StringContent(proteins), "Proteins");
        updateForm.Add(new StringContent(fats), "Fats");
        updateForm.Add(new StringContent(carbs), "Carbohydrates");
        updateForm.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        updateForm.Add(new StringContent("100"), "Ingredients[0].Amount");

        var response = await _client.PutAsync($"/api/dish/{created!.Id}", updateForm);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDish_NonExistingId_ReturnsNotFound()
    {
        var productId = await CreateProductAsync();

        var form = new MultipartFormDataContent();
        form.Add(new StringContent("Неважно"), "Name");
        form.Add(new StringContent("200"), "PortionSize");
        form.Add(new StringContent("0"), "Category");
        form.Add(new StringContent($"{productId}"), "Ingredients[0].ProductId");
        form.Add(new StringContent("150"), "Ingredients[0].Amount");

        var response = await _client.PutAsync($"/api/dish/{Guid.NewGuid()}", form);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("category=0")]
    [InlineData("category=3")]
    public async Task GetDishes_FilterByCategory_ReturnsFiltered(string filter)
    {
        var response = await _client.GetAsync($"/api/dish?{filter}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
    }

    [Theory]
    [InlineData("search=Тест")]
    [InlineData("search=zzzНеСуществует")]
    [InlineData("search=")]
    public async Task GetDishes_SearchByName_ReturnsOk(string filter)
    {
        var response = await _client.GetAsync($"/api/dish?{filter}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
    }

    [Theory]
    [InlineData("sortBy=name&ascending=true")]
    [InlineData("sortBy=name&ascending=false")]
    [InlineData("sortBy=calories&ascending=true")]
    [InlineData("sortBy=calories&ascending=false")]
    [InlineData("sortBy=proteins")]
    [InlineData("sortBy=fats")]
    [InlineData("sortBy=carbs")]
    public async Task GetDishes_SortBy_ReturnsOk(string sort)
    {
        var response = await _client.GetAsync($"/api/dish?{sort}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
    }

    [Fact]
    public async Task GetDishes_FilterByDietaryFlags_ReturnsFiltered()
    {
        var response = await _client.GetAsync("/api/dish?dietaryFlags=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
        Assert.All(dishes, d => Assert.NotEqual(0, (int)d.DietaryFlags & 1));
    }

    [Theory]
    [InlineData("category=0&search=Тест")]
    [InlineData("category=0&sortBy=calories")]
    [InlineData("dietaryFlags=1&sortBy=name")]
    public async Task GetDishes_CombinedFilters_ReturnsOk(string filters)
    {
        var response = await _client.GetAsync($"/api/dish?{filters}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dishes = await response.Content.ReadFromJsonAsync<List<Dish>>();
        Assert.NotNull(dishes);
    }
}