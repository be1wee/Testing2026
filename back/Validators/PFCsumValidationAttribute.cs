using System.ComponentModel.DataAnnotations;
using back.Models;

namespace back.Validators
{
    public class PFCsumValidationAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            var dto = value as ProductRequestDto;
            if (dto == null) return ValidationResult.Success;
            
            if (dto.Proteins + dto.Fats + dto.Carbohydrates > 100)
                return new ValidationResult("Сумма белков, жиров и углеводов не может превышать 100г");
            else
                return ValidationResult.Success;
        }
    }
}