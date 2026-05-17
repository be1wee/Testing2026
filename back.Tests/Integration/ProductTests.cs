using System.Net;
using System.Net.Http.Json;
using back.Enums;
using back.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace back.Tests.Integration;


public class ProductTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductTests(WebApplicationFactory<Program> factory)
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
        Console.Write(response);
        Console.Write(product);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);  
        Assert.NotNull(product);                                
        Assert.Equal("Тестовый продукт", product.Name);        
        Assert.Equal(150, product.Calories);                    
    }

}