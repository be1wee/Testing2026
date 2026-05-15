using System.ComponentModel.DataAnnotations;
using back.Enums;

namespace back.Models
{
    public class DishRequestDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(200)]
        public required string Name { get; set; }

        [MaxLength(5)]
        public List<string>? ImageUrl { get; set; }

        public double? Calories { get; set; }
        public double? Proteins { get; set; }
        public double? Fats { get; set; }
        public double? Carbohydrates { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Должен быть минимум 1 ингредиент")]
        public required List<DishIngredientDto> Ingredients { get; set; }

        [Required]
        [Range(0.1, double.MaxValue)]
        public required double PortionSize { get; set; }

        public DishCategory? Category { get; set; }
        public DietaryFlags? DietaryFlags { get; set; }
        public List<IFormFile>? Images { get; set; }
    }

    public class DishIngredientDto
    {
        [Required]
        public required Guid ProductId { get; set; }

        [Required]
        [Range(0.1, double.MaxValue)]
        public required double Amount { get; set; }
    }
}