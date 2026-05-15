using System.ComponentModel.DataAnnotations;
using back.Enums;

namespace back.Models
{
    public class Dish
    {
        public Guid Id { get; set; }

        [Required]
        [MinLength(2)]
        [MaxLength(200)]
        public required string Name { get; set; }

        [MaxLength(5)]
        public List<string> ImageUrl { get; set; } = new();

        [Required]
        [Range(0, double.MaxValue)]
        public required double Calories { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public required double Proteins { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public required double Fats { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public required double Carbohydrates { get; set; }

        [Required]
        public List<DishesProduct> DishesProducts { get; set; } = new();

        [Required]
        [Range(0.1, double.MaxValue)]
        public required double PortionSize { get; set; }

        [Required]
        public required DishCategory Category { get; set; }

        public DietaryFlags DietaryFlags { get; set; } = DietaryFlags.None;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}