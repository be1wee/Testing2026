using back.Data;
using back.Models;

namespace back.Helpers
{
    public static class DishCalculator
    {
        
        public static (double calories, double proteins, double fats, double carbs) Calculate(Dish dish)
        {
            double calories = 0, proteins = 0, fats = 0, carbs = 0;
            
            foreach (var dp in dish.DishesProducts)  
            {
                if (dp.Product == null) continue;
                
                var factor = dp.Amount / 100;
                calories += dp.Product.Calories * factor;
                proteins += dp.Product.Proteins * factor;
                fats += dp.Product.Fats * factor;
                carbs += dp.Product.Carbohydrates * factor;
            }
            
            return (calories, proteins, fats, carbs);
        }
    }
}