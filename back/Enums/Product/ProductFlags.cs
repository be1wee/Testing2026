using System.ComponentModel.DataAnnotations;

namespace back.Enums
{
    [Flags]
    public enum DietaryFlags
    {
        [Display(Name = "Без флагов")]
        None = 0,
        
        [Display(Name = "Веган")]
        Vegan = 1,
        
        [Display(Name = "Без глютена")]
        GlutenFree = 2,
        
        [Display(Name = "Без сахара")]
        SugarFree = 4
    }
}