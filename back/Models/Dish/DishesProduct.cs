using System.ComponentModel.DataAnnotations;

namespace back.Models
{
    public class DishesProduct
    {
        public Guid Id { get; set; }
        
        public Guid DishId { get; set; }
        public Dish? Dish { get; set; }
        
        public Guid ProductId { get; set; }
        public Product? Product { get; set; }
        
        [Required]
        [Range(0.1, double.MaxValue)]
        public required double Amount { get; set; }
    }
}