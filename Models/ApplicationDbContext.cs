using Microsoft.EntityFrameworkCore;
using System;

namespace projet_info_finale.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; }
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure OrderStatus enum conversion
            modelBuilder.Entity<Order>(entity =>
            {
                entity.Property(o => o.OrderStatus)
                    .HasConversion(
                        v => v == OrderStatus.OutForDelivery ? "Out for Delivery" : v.ToString(), // Custom conversion for OutForDelivery
                        v => v == "Out for Delivery" ? OrderStatus.OutForDelivery : (OrderStatus)Enum.Parse(typeof(OrderStatus), v)
                    )
                    .IsRequired(); // Ensure this column cannot be null

                entity.Property(o => o.TotalPrice)
                    .HasColumnType("decimal(18,2)") // Define precision and scale for monetary values
                    .IsRequired();
            });

            // Define relationships or constraints if needed
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(oi => oi.OrderID)
                      .OnDelete(DeleteBehavior.Cascade); // Cascading delete for related OrderItems

                entity.HasOne(oi => oi.MenuItem)
                      .WithMany()
                      .HasForeignKey(oi => oi.MenuItemID);
            });

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(u => u.UserType)
                    .HasConversion(
                        v => v.ToString(), // Enum to string for database storage
                        v => (UserType)Enum.Parse(typeof(UserType), v) // String to enum for application use
                    )
                    .IsRequired(); // Ensure this column cannot be null
            });
        }
    }
}
