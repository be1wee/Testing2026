using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using back.Enums;
using back.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace back.Tests.Integration;


public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_ValidData_ReturnsOk()
    {
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("Тестовый продукт"), "Name");  
        formData.Add(new StringContent("150"), "Calories");            
        formData.Add(new StringContent("10"), "Proteins");             
        formData.Add(new StringContent("5"), "Fats");                  
        formData.Add(new StringContent("20"), "Carbohydrates");        
        formData.Add(new StringContent("0"), "Category");              
        formData.Add(new StringContent("0"), "Readiness");             
        formData.Add(new StringContent("0"), "DietaryFlags");          

        
        var response = await _client.PostAsync("/api/product", formData);
        var product = await response.Content.ReadFromJsonAsync<Product>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);  
        Assert.NotNull(product);                                
        Assert.Equal("Тестовый продукт", product.Name);        
        Assert.Equal(150, product.Calories);                    
    }

    [Theory]
    [InlineData("Тестовый продукт", HttpStatusCode.OK)]    
    [InlineData("Аб", HttpStatusCode.OK)]                  //минимум   
    [InlineData("А", HttpStatusCode.BadRequest)]            //меньше минимума 
    public async Task CreateProduct_NameLength_ReturnsExpectedStatus(string name, HttpStatusCode expectedStatus)
    {
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(name), "Name");
        formData.Add(new StringContent("100"), "Calories");
        formData.Add(new StringContent("10"), "Proteins");
        formData.Add(new StringContent("5"), "Fats");
        formData.Add(new StringContent("20"), "Carbohydrates");
        formData.Add(new StringContent("0"), "Category");
        formData.Add(new StringContent("0"), "Readiness");
        formData.Add(new StringContent("0"), "DietaryFlags");

        var response = await _client.PostAsync("/api/product", formData);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("0", "0", "0", HttpStatusCode.OK)]            // нулевое кбжу
    [InlineData("40", "30", "30", HttpStatusCode.OK)]          // сумма = 100
    [InlineData("50", "50", "50", HttpStatusCode.BadRequest)]  // сумма 150 > 100
    [InlineData("100", "0", "0", HttpStatusCode.OK)]           // белки = 100
    [InlineData("0", "100", "0", HttpStatusCode.OK)]           // жиры = 100
    [InlineData("0", "0", "100", HttpStatusCode.OK)]           // углеводы = 100
    [InlineData("-1", "0", "0", HttpStatusCode.BadRequest)]    // отрицательные белки
    [InlineData("0", "-1", "0", HttpStatusCode.BadRequest)]    // отрицательные жиры
    [InlineData("0", "0", "-1", HttpStatusCode.BadRequest)]    // отрицательные углеводы
    [InlineData("-10", "-10", "-10", HttpStatusCode.BadRequest)] // всё отрицательное
    public async Task CreateProduct_CPFC_ReturnsExpectedStatus(
        string proteins, string fats, string carbs, HttpStatusCode expectedStatus)
    {
        var formData = new MultipartFormDataContent();
        formData.Add(new StringContent("Тест БЖУ"), "Name");
        formData.Add(new StringContent("200"), "Calories");
        formData.Add(new StringContent(proteins), "Proteins");
        formData.Add(new StringContent(fats), "Fats");
        formData.Add(new StringContent(carbs), "Carbohydrates");
        formData.Add(new StringContent("0"), "Category");
        formData.Add(new StringContent("0"), "Readiness");
        formData.Add(new StringContent("0"), "DietaryFlags");

        var response = await _client.PostAsync("/api/product", formData);

        Assert.Equal(expectedStatus, response.StatusCode);
    }



    [Fact]
    public async Task GetProductById_ExistingId_ReturnsProduct()
    {
        
        var createForm = new MultipartFormDataContent();
        createForm.Add(new StringContent("Для получения"), "Name");
        createForm.Add(new StringContent("100"), "Calories");
        createForm.Add(new StringContent("5"), "Proteins");
        createForm.Add(new StringContent("2"), "Fats");
        createForm.Add(new StringContent("10"), "Carbohydrates");
        createForm.Add(new StringContent("0"), "Category");
        createForm.Add(new StringContent("0"), "Readiness");
        createForm.Add(new StringContent("0"), "DietaryFlags");

        var createResponse = await _client.PostAsync("/api/product", createForm);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

       
        var response = await _client.GetAsync($"/api/product/{created!.Id}");
        var product = await response.Content.ReadFromJsonAsync<Product>();
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(product);
        Assert.Equal(created.Id, product.Id);
        Assert.Equal("Для получения", product.Name);
        Assert.Equal(100, product.Calories);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/product/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProduct_ExistingId_ReturnsOk()
    {
        var createForm = new MultipartFormDataContent();
        createForm.Add(new StringContent("Для удаления"), "Name");
        createForm.Add(new StringContent("100"), "Calories");
        createForm.Add(new StringContent("5"), "Proteins");
        createForm.Add(new StringContent("2"), "Fats");
        createForm.Add(new StringContent("10"), "Carbohydrates");
        createForm.Add(new StringContent("0"), "Category");
        createForm.Add(new StringContent("0"), "Readiness");
        createForm.Add(new StringContent("0"), "DietaryFlags");

        var createResponse = await _client.PostAsync("/api/product", createForm);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var deleteResponse = await _client.DeleteAsync($"/api/product/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/product/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }


    [Fact]
    public async Task DeleteProduct_NonExistingId_ReturnsNotFound()
    {
        var response = await _client.DeleteAsync($"/api/product/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }



    [Theory]
    [InlineData("После обновления", "200", "10", "4", "20", HttpStatusCode.OK)]           // обычное обновление
    [InlineData("Аб", "150", "5", "5", "10", HttpStatusCode.OK)]                           // мин. имя 2 символа
    [InlineData("А", "150", "5", "5", "10", HttpStatusCode.BadRequest)]                    // имя 1 символ
    [InlineData("Вода", "0", "0", "0", "0", HttpStatusCode.OK)]                            // нулевые КБЖУ
    [InlineData("Сумма 100", "300", "40", "30", "30", HttpStatusCode.OK)]                  // сумма БЖУ = 100
    [InlineData("Сумма >100", "300", "50", "50", "50", HttpStatusCode.BadRequest)]         // сумма БЖУ > 100
    [InlineData("Отриц белки", "100", "-1", "0", "0", HttpStatusCode.BadRequest)]          // отрицательные белки
    [InlineData("Отриц жиры", "100", "0", "-1", "0", HttpStatusCode.BadRequest)]           // отрицательные жиры
    [InlineData("Отриц углев", "100", "0", "0", "-1", HttpStatusCode.BadRequest)]          // отрицательные углеводы
    public async Task UpdateProduct_NutrientBoundaries_ReturnsExpectedStatus(
        string name, string calories, string proteins, string fats, string carbs, HttpStatusCode expectedStatus)
    {
        var createForm = new MultipartFormDataContent();
        createForm.Add(new StringContent("До обновления"), "Name");
        createForm.Add(new StringContent("100"), "Calories");
        createForm.Add(new StringContent("5"), "Proteins");
        createForm.Add(new StringContent("2"), "Fats");
        createForm.Add(new StringContent("10"), "Carbohydrates");
        createForm.Add(new StringContent("0"), "Category");
        createForm.Add(new StringContent("0"), "Readiness");
        createForm.Add(new StringContent("0"), "DietaryFlags");

        var createResponse = await _client.PostAsync("/api/product", createForm);
        var created = await createResponse.Content.ReadFromJsonAsync<Product>();

        var updateForm = new MultipartFormDataContent();
        updateForm.Add(new StringContent(name), "Name");
        updateForm.Add(new StringContent(calories), "Calories");
        updateForm.Add(new StringContent(proteins), "Proteins");
        updateForm.Add(new StringContent(fats), "Fats");
        updateForm.Add(new StringContent(carbs), "Carbohydrates");
        updateForm.Add(new StringContent("0"), "Category");
        updateForm.Add(new StringContent("0"), "Readiness");
        updateForm.Add(new StringContent("0"), "DietaryFlags");

        var response = await _client.PutAsync($"/api/product/{created!.Id}", updateForm);

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProduct_NonExistingId_ReturnsNotFound()
    {
        var updateForm = new MultipartFormDataContent();
        updateForm.Add(new StringContent("Неважно"), "Name");
        updateForm.Add(new StringContent("100"), "Calories");
        updateForm.Add(new StringContent("5"), "Proteins");
        updateForm.Add(new StringContent("2"), "Fats");
        updateForm.Add(new StringContent("10"), "Carbohydrates");
        updateForm.Add(new StringContent("0"), "Category");
        updateForm.Add(new StringContent("0"), "Readiness");
        updateForm.Add(new StringContent("0"), "DietaryFlags");

        var response = await _client.PutAsync($"/api/product/{Guid.NewGuid()}", updateForm);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }




    [Theory]
    [InlineData("category=0", HttpStatusCode.OK)]        // существующая
    [InlineData("category=3", HttpStatusCode.OK)]        // отличная от сущ-ей
    [InlineData("category=999", HttpStatusCode.BadRequest)]      // несуществующая
    public async Task GetProducts_FilterByCategory_ReturnsOk(string filter, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync($"/api/product/filter?{filter}");

        Assert.Equal(expectedStatus, response.StatusCode);
         if (expectedStatus == HttpStatusCode.OK)
        {
            var products = await response.Content.ReadFromJsonAsync<List<Product>>();
            Assert.NotNull(products);
        }
    }

    [Theory]
    [InlineData("readiness=0", HttpStatusCode.OK)]       // готов
    [InlineData("readiness=2", HttpStatusCode.OK)]       // треб пригот
    public async Task GetProducts_FilterByReadiness_ReturnsOk(string filter, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync($"/api/product/filter?{filter}");

        Assert.Equal(expectedStatus, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
    }

    [Theory]
    [InlineData("search=Тест", true)]                    // подстрока есть
    [InlineData("search=zzzНеСуществует", true)]          // подстрока не сущ
    [InlineData("search=", true)]                        // пустая стр
    public async Task GetProducts_SearchByName_ReturnsOk(string filter, bool canBeEmpty)
    {
        var response = await _client.GetAsync($"/api/product/filter?{filter}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
        if (!canBeEmpty) Assert.NotEmpty(products);
    }

    [Theory]
    [InlineData("sortBy=name", HttpStatusCode.OK)]
    [InlineData("sortBy=calories", HttpStatusCode.OK)]
    [InlineData("sortBy=proteins", HttpStatusCode.OK)]
    [InlineData("sortBy=fats", HttpStatusCode.OK)]
    [InlineData("sortBy=carbohydrates", HttpStatusCode.OK)]
    [InlineData("sortBy=calories&ascending=true", HttpStatusCode.OK)]
    [InlineData("sortBy=calories&ascending=false", HttpStatusCode.OK)]
    public async Task GetProducts_SortBy_ReturnsOk(string sort, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync($"/api/product/filter?{sort}");

        Assert.Equal(expectedStatus, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
    }

    [Theory]
    [InlineData("category=0&readiness=0&search=Тест", HttpStatusCode.OK)]
    [InlineData("category=0&sortBy=calories", HttpStatusCode.OK)]
    [InlineData("readiness=0&sortBy=name&ascending=false", HttpStatusCode.OK)]
    public async Task GetProducts_CombinedFilters_ReturnsOk(string filters, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync($"/api/product/filter?{filters}");

        Assert.Equal(expectedStatus, response.StatusCode);
        var products = await response.Content.ReadFromJsonAsync<List<Product>>();
        Assert.NotNull(products);
    }


}

