using System.ComponentModel.DataAnnotations;

namespace back.Enums
{   
    public enum ProductCategory
    {
        [Display(Name = "Замороженный")]
        Frozen,
        
        [Display(Name = "Мясной")]
        Meat,
        
        [Display(Name = "Овощи")]
        Vegetables,
        
        [Display(Name = "Зелень")]
        Herbs,
        
        [Display(Name = "Специи")]
        Spices,
        
        [Display(Name = "Крупы")]
        Cereals,
        
        [Display(Name = "Консервы")]
        CannedGoods,
        
        [Display(Name = "Жидкость")]
        Liquids,
        
        [Display(Name = "Сладости")]
        Sweets
    }
}
