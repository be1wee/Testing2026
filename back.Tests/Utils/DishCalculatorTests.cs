using back.Helpers;
using back.Models;

namespace back.Tests.Utils;

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
    public void TwoProducts_SumsValues()
    {
        var p1 = CreateProduct(calories: 100, proteins: 10, fats: 5, carbohydrates: 20);
        var p2 = CreateProduct(calories: 200, proteins: 25, fats: 10, carbohydrates: 0);
        var dish = CreateDish(new List<(Product, double)> { (p1, 100), (p2, 50) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(200, result.calories);
        Assert.Equal(22.5, result.proteins);
        Assert.Equal(10, result.fats);
        Assert.Equal(20, result.carbs);
    }


    [Theory]
    [InlineData(100, 200, 25, 10, 0, 200, 25, 10, 0)]
    [InlineData(50, 200, 20, 10, 30, 100, 10, 5, 15)]
    [InlineData(0, 500, 10, 5, 20, 0, 0, 0, 0)]
    public void ProductScaling_ReturnsCorrectValues(
        double amount,
        double cal, double prot, double fat, double carb,
        double expectedCal, double expectedProt, double expectedFat, double expectedCarb)
    {
        var product = CreateProduct(calories: cal, proteins: prot, fats: fat, carbohydrates: carb);
        var dish = CreateDish(new List<(Product, double)> { (product, amount) });

        var result = DishCalculator.Calculate(dish);

        Assert.Equal(expectedCal, result.calories);
        Assert.Equal(expectedProt, result.proteins);
        Assert.Equal(expectedFat, result.fats);
        Assert.Equal(expectedCarb, result.carbs);
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