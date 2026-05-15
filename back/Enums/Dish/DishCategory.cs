using System.ComponentModel.DataAnnotations;

namespace back.Enums
{
    public enum DishCategory
    {
        [Display(Name = "Десерт")]
        Dessert,
        
        [Display(Name = "Первое")]
        First,
        
        [Display(Name = "Второе")]
        Second,
        
        [Display(Name = "Напиток")]
        Drink,
        
        [Display(Name = "Салат")]
        Salad,
        
        [Display(Name = "Суп")]
        Soup,
        
        [Display(Name = "Перекус")]
        Snack
    }
}