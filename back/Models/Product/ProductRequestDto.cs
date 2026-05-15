using System.ComponentModel.DataAnnotations;
using back.Enums;

namespace back.Models
{
    public class ProductRequestDto
    {
        [Required]
        [MinLength(2)]
        [MaxLength(200)]
        public required string Name { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public required double Calories { get; set; }

        [Required]
        [Range(0, 100)]
        public required double Proteins { get; set; }

        [Required]
        [Range(0, 100)]
        public required double Fats { get; set; }

        [Required]
        [Range(0, 100)]
        public required double Carbohydrates { get; set; }

        public string? Composition { get; set; }

        [Required]
        public required ProductCategory Category { get; set; }

        [Required]
        public required Readiness Readiness { get; set; }

        [MaxLength(5)]
        public List<IFormFile> Images { get; set; } = new();

        public DietaryFlags DietaryFlags { get; set; } = DietaryFlags.None;
    }
}