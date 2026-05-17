// using System.Net;
// using System.Net.Http.Json;
// using back.Enums;
// using back.Models;
// using Microsoft.AspNetCore.Mvc.Testing;

// namespace back.Tests.Integration;

// /// <summary>
// /// Интеграционные тесты для Product API.
// /// Проверяют реальные HTTP-запросы к серверу без изоляции зависимостей.
// /// 
// /// Техники тест-дизайна:
// /// - Эквивалентное разбиение (создание, получение, удаление)
// /// - Анализ граничных значений (пустое имя, нулевые БЖУ, максимальные значения)
// /// </summary>
// public class ProductApiTests : IClassFixture<WebApplicationFactory<Program>>
// {
//     private readonly HttpClient _client;

//     public ProductApiTests(WebApplicationFactory<Program> factory)
//     {
//         _client = factory.CreateClient();
//     }

//     // ======================
//     // Эквивалентное разбиение
//     // ======================

//     /// <summary>
//     /// Класс эквивалентности: создание продукта с валидными данными.
//     /// Ожидаем: 200 OK и корректные данные в ответе.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_ValidData_ReturnsOk()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Тестовый продукт"), "Name");
//         formData.Add(new StringContent("150"), "Calories");
//         formData.Add(new StringContent("10"), "Proteins");
//         formData.Add(new StringContent("5"), "Fats");
//         formData.Add(new StringContent("20"), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);
//         var product = await response.Content.ReadFromJsonAsync<Product>();

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         Assert.NotNull(product);
//         Assert.Equal("Тестовый продукт", product!.Name);
//         Assert.Equal(150, product.Calories);
//         Assert.Equal(10, product.Proteins);
//         Assert.Equal(5, product.Fats);
//         Assert.Equal(20, product.Carbohydrates);
//     }

//     /// <summary>
//     /// Класс эквивалентности: получение списка продуктов.
//     /// Ожидаем: 200 OK и непустой список.
//     /// </summary>
//     [Fact]
//     public async Task GetProducts_ReturnsList()
//     {
//         // Act
//         var response = await _client.GetAsync("/api/product");

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         var products = await response.Content.ReadFromJsonAsync<List<Product>>();
//         Assert.NotNull(products);
//     }

//     /// <summary>
//     /// Класс эквивалентности: получение продукта по ID.
//     /// Ожидаем: 200 OK и правильный продукт.
//     /// </summary>
//     [Fact]
//     public async Task GetProductById_ExistingId_ReturnsProduct()
//     {
//         // Arrange — сначала создаём продукт
//         var createForm = new MultipartFormDataContent();
//         createForm.Add(new StringContent("Для получения"), "Name");
//         createForm.Add(new StringContent("100"), "Calories");
//         createForm.Add(new StringContent("5"), "Proteins");
//         createForm.Add(new StringContent("2"), "Fats");
//         createForm.Add(new StringContent("10"), "Carbohydrates");
//         createForm.Add(new StringContent("0"), "Category");
//         createForm.Add(new StringContent("0"), "Readiness");
//         createForm.Add(new StringContent("0"), "DietaryFlags");

//         var createResponse = await _client.PostAsync("/api/product", createForm);
//         var created = await createResponse.Content.ReadFromJsonAsync<Product>();

//         // Act
//         var response = await _client.GetAsync($"/api/product/{created!.Id}");

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         var product = await response.Content.ReadFromJsonAsync<Product>();
//         Assert.Equal(created.Id, product!.Id);
//         Assert.Equal("Для получения", product.Name);
//     }

//     /// <summary>
//     /// Класс эквивалентности: несуществующий ID → 404.
//     /// </summary>
//     [Fact]
//     public async Task GetProductById_NonExistingId_ReturnsNotFound()
//     {
//         // Act
//         var response = await _client.GetAsync($"/api/product/{Guid.NewGuid()}");

//         // Assert
//         Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
//     }

//     /// <summary>
//     /// Класс эквивалентности: фильтрация по категории.
//     /// Ожидаем: только продукты указанной категории.
//     /// </summary>
//     [Fact]
//     public async Task GetProducts_FilterByCategory_ReturnsFilteredList()
//     {
//         // Act
//         var response = await _client.GetAsync("/api/product/filter?category=0");

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//         var products = await response.Content.ReadFromJsonAsync<List<Product>>();
//         Assert.NotNull(products);
//         Assert.All(products, p => Assert.Equal(0, (int)p.Category));
//     }

//     // ======================
//     // Анализ граничных значений
//     // ======================

//     /// <summary>
//     /// Граничное значение: имя продукта из 2 символов (минимальная длина).
//     /// Ожидаем: 200 OK.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_MinNameLength_ReturnsOk()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Аб"), "Name"); // ровно 2 символа
//         formData.Add(new StringContent("100"), "Calories");
//         formData.Add(new StringContent("10"), "Proteins");
//         formData.Add(new StringContent("5"), "Fats");
//         formData.Add(new StringContent("20"), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//     }

//     /// <summary>
//     /// Граничное значение: имя из 1 символа (ниже минимальной длины).
//     /// Ожидаем: 400 Bad Request.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_NameTooShort_ReturnsBadRequest()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("А"), "Name"); // 1 символ
//         formData.Add(new StringContent("100"), "Calories");
//         formData.Add(new StringContent("10"), "Proteins");
//         formData.Add(new StringContent("5"), "Fats");
//         formData.Add(new StringContent("20"), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
//     }

//     /// <summary>
//     /// Граничное значение: нулевые калории.
//     /// Ожидаем: 200 OK.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_ZeroCalories_ReturnsOk()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Вода"), "Name");
//         formData.Add(new StringContent("0"), "Calories");
//         formData.Add(new StringContent("0"), "Proteins");
//         formData.Add(new StringContent("0"), "Fats");
//         formData.Add(new StringContent("0"), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//     }

//     /// <summary>
//     /// Граничное значение: сумма БЖУ = 100 (верхняя граница).
//     /// Ожидаем: 200 OK.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_MaxNutrientsSum_ReturnsOk()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Максимум БЖУ"), "Name");
//         formData.Add(new StringContent("500"), "Calories");
//         formData.Add(new StringContent("40"), "Proteins");
//         formData.Add(new StringContent("30"), "Fats");
//         formData.Add(new StringContent("30"), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(HttpStatusCode.OK, response.StatusCode);
//     }

//     /// <summary>
//     /// Граничное значение: сумма БЖУ > 100 (превышение).
//     /// Ожидаем: 400 Bad Request.
//     /// </summary>
//     [Fact]
//     public async Task CreateProduct_NutrientSumExceeds100_ReturnsBadRequest()
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Перебор БЖУ"), "Name");
//         formData.Add(new StringContent("500"), "Calories");
//         formData.Add(new StringContent("50"), "Proteins");
//         formData.Add(new StringContent("50"), "Fats");
//         formData.Add(new StringContent("50"), "Carbohydrates"); // сумма 150 > 100
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
//     }

//     /// <summary>
//     /// Параметризованный тест: разные значения БЖУ.
//     /// Проверяет граничные и обычные значения.
//     /// </summary>
//     [Theory]
//     [InlineData("0", "0", "0", HttpStatusCode.OK)]       // всё по нулям
//     [InlineData("40", "30", "30", HttpStatusCode.OK)]     // сумма = 100
//     [InlineData("100", "0", "0", HttpStatusCode.OK)]      // только белки 100
//     [InlineData("0", "100", "0", HttpStatusCode.OK)]      // только жиры 100
//     [InlineData("0", "0", "100", HttpStatusCode.OK)]      // только углеводы 100
//     [InlineData("50", "50", "50", HttpStatusCode.BadRequest)] // сумма 150
//     public async Task CreateProduct_NutrientBoundaries_ReturnsExpectedStatus(
//         string proteins, string fats, string carbs, HttpStatusCode expectedStatus)
//     {
//         // Arrange
//         var formData = new MultipartFormDataContent();
//         formData.Add(new StringContent("Тест БЖУ"), "Name");
//         formData.Add(new StringContent("200"), "Calories");
//         formData.Add(new StringContent(proteins), "Proteins");
//         formData.Add(new StringContent(fats), "Fats");
//         formData.Add(new StringContent(carbs), "Carbohydrates");
//         formData.Add(new StringContent("0"), "Category");
//         formData.Add(new StringContent("0"), "Readiness");
//         formData.Add(new StringContent("0"), "DietaryFlags");

//         // Act
//         var response = await _client.PostAsync("/api/product", formData);

//         // Assert
//         Assert.Equal(expectedStatus, response.StatusCode);
//     }
// }