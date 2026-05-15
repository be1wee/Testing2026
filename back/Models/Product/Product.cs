// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using back.Enums;

namespace back.Models
{
    public class Product
    {
        public Guid Id { get; set; }

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
        public List<string> ImageUrl { get; set; } = new();

        public DietaryFlags DietaryFlags { get; set; } = DietaryFlags.None;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public List<DishesProduct> DishesProducts { get; set; } = new();
    }
}