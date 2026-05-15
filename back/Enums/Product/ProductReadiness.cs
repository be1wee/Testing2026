using System.ComponentModel.DataAnnotations;

namespace back.Enums
{   
    public enum Readiness
    {
        [Display(Name = "Готов к употреблению")]
        ReadyToEat,
        [Display(Name = "Полуфабрикат")]
        SemiCooked,
        [Display(Name = "Требует приготовления")]
        RequiresCooking
    }
}
