using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SweetCakeShop.Models;

namespace SweetCakeShop.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; } // NEW
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponCustomer> CouponCustomers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // config relationship nếu cần
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId);

            // set precision / column types to avoid silent truncation
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderDetail>()
                .Property(od => od.Price)
                .HasPrecision(18, 2);

            builder.Entity<Ingredient>()
                .Property(i => i.Quantity)
                .HasPrecision(10, 2);

            builder.Entity<Recipe>(entity =>
            {
                entity.ToTable("Recipe");

                entity.HasKey(r => r.RecipeID);

                entity.Property(r => r.Quantity)
                      .HasPrecision(10, 2);

                entity.HasOne(r => r.Product)
                    .WithMany()
                    .HasForeignKey(r => r.ProductID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Ingredient)
                    .WithMany()
                    .HasForeignKey(r => r.IngredientsID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(r => new { r.ProductID, r.IngredientsID }).IsUnique();
            });
            builder.Entity<ProductReview>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasOne(r => r.Order)
                .WithMany()
                .HasForeignKey(r => r.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ContactMessage>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Coupon relationships
            builder.Entity<Coupon>()
                .HasKey(c => c.CouponId);

            builder.Entity<Coupon>()
                .Property(c => c.DiscountPercent)
                .HasPrecision(5, 2);

            builder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            builder.Entity<CouponCustomer>()
                .HasKey(cc => cc.CouponCustomerId);

            builder.Entity<CouponCustomer>()
                .HasOne(cc => cc.Coupon)
                .WithMany(c => c.CouponCustomers)
                .HasForeignKey(cc => cc.CouponId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CouponCustomer>()
                .HasOne(cc => cc.Customer)
                .WithMany()
                .HasForeignKey(cc => cc.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order to Coupon relationship
            builder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CouponId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}