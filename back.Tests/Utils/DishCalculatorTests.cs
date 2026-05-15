using back.Helpers;
using back.Models;

namespace back.Tests.Helpers;

public class DishCalculatorTests
{
    private static Product CreateProduct(
        double calories = 100,
        double proteins = 10,
        double fats = 5,
        double carbohydrates = 20)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Calories = calories,
            Proteins = proteins,
            Fats = fats,
            Carbohydrates = carbohydrates,
            Category = back.Enums.ProductCategory.Vegetables,
            Readiness = back.Enums.Readiness.ReadyToEat
        };
    }

    private static Dish CreateDish(List<(Product product, double amount)> ingredients)
    {
        var dish = new Dish
        {
            Id = Guid.NewGuid(),
            Name = "Test Dish",
            Calories = 0,
            Proteins = 0,
            Fats = 0,
            Carbohydrates = 0,
            PortionSize = 200,
            Category = back.Enums.DishCategory.Second
        };

        foreach (var (product, amount) in ingredients)
        {
            dish.DishesProducts.Add(new DishesProduct
            {
                Id = Guid.NewGuid(),
                DishId = dish.Id,
                ProductId = product.Id,
                Product = product,
                Amount = amount
            });
        }

        return dish;
    }

     
    [Fact]
    public void EmptyIngredients_ReturnsZero()
    {
        var dish = CreateDish(new List<(Product, double)>());

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(0, result.calories);
        Assert.Equal(0, result.proteins);
        Assert.Equal(0, result.fats);
        Assert.Equal(0, result.carbs);
    }



    [Fact]
    public void OneProduct100g_ReturnsProductValues()
    {
        var product = CreateProduct(calories: 200, proteins: 25, fats: 10, carbohydrates: 0);
        var dish = CreateDish(new List<(Product, double)> { (product, 100) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(200, result.calories);
        Assert.Equal(25, result.proteins);
        Assert.Equal(10, result.fats);
        Assert.Equal(0, result.carbs);
    }

    [Fact]
    public void TwoProducts_SumsValues()
    {
        var p1 = CreateProduct(calories: 100, proteins: 10, fats: 5, carbohydrates: 20);
        var p2 = CreateProduct(calories: 200, proteins: 25, fats: 10, carbohydrates: 0);
        var dish = CreateDish(new List<(Product, double)> { (p1, 100), (p2, 50) });

        var result = DishCalculator.Calculate(dish);

  
        Assert.Equal(100 + 100, result.calories);  
        Assert.Equal(10 + 12.5, result.proteins);    
        Assert.Equal(5 + 5, result.fats);            
        Assert.Equal(20 + 0, result.carbs);          
    }


    [Fact]
    public void ZeroAmount_IgnoresIngredient()
    {
        var product = CreateProduct(calories: 500);
        var dish = CreateDish(new List<(Product, double)> { (product, 0) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(0, result.calories);
    }


    [Fact]
    public void ZeroCalPFCProduct_AddsNothing()
    {
        var product = CreateProduct(calories: 0, proteins: 0, fats: 0, carbohydrates: 0);
        var dish = CreateDish(new List<(Product, double)> { (product, 500) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(0, result.calories);
        Assert.Equal(0, result.proteins);
        Assert.Equal(0, result.fats);
        Assert.Equal(0, result.carbs);
    }


    [Fact]
    public void VerySmallAmount_CorrectFraction()
    {
        var product = CreateProduct(calories: 300, proteins: 10, fats: 5, carbohydrates: 50);
        var dish = CreateDish(new List<(Product, double)> { (product, 0.1) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(0.3, result.calories);
        Assert.Equal(0.01, result.proteins);
        Assert.Equal(0.005, result.fats);
        Assert.Equal(0.05, result.carbs);
    }

 
    [Fact]
    public void VeryLargeAmount_CorrectScaling()
    {
        var product = CreateProduct(calories: 350, proteins: 10, fats: 1, carbohydrates: 70);
        var dish = CreateDish(new List<(Product, double)> { (product, 10000) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(35000, result.calories);
        Assert.Equal(1000, result.proteins);
        Assert.Equal(100, result.fats);
        Assert.Equal(7000, result.carbs);
    }
}