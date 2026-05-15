using back.Models;
using Microsoft.EntityFrameworkCore;
namespace back.Data
{
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
            : base(options)
        {
        }
        public DbSet<Product> Products { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<DishesProduct> DishesProducts { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.Entity<Product>(entity =>
            {
                entity.Property(p => p.ImageUrl)
                    .HasColumnType("text[]");
            });

            modelBuilder.Entity<Dish>(entity =>
            {
                entity.Property(d => d.ImageUrl)
                    .HasColumnType("text[]");
            });

            modelBuilder.Entity<DishesProduct>(entity =>
            {
                entity.HasOne(dp => dp.Dish)
                    .WithMany(d => d.DishesProducts)
                    .HasForeignKey(dp => dp.DishId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(dp => dp.Product)
                    .WithMany(p => p.DishesProducts)
                    .HasForeignKey(dp => dp.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}


