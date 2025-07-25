using DFTRK.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DFTRK.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<WholesalerProduct> WholesalerProducts { get; set; }
        public DbSet<RetailerProduct> RetailerProducts { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<RetailerPartnership> RetailerPartnerships { get; set; }
        public DbSet<RetailerPartnerCategory> RetailerPartnerCategories { get; set; }
        public DbSet<RetailerPartnerProduct> RetailerPartnerProducts { get; set; }
        public DbSet<WholesalerExternalRetailer> WholesalerExternalRetailers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            
            // Category - CreatedBy
            modelBuilder.Entity<Category>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Product - Category
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // WholesalerProduct - Product
            modelBuilder.Entity<WholesalerProduct>()
                .HasOne(wp => wp.Product)
                .WithMany(p => p.WholesalerProducts)
                .HasForeignKey(wp => wp.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // WholesalerProduct - Wholesaler (ApplicationUser)
            modelBuilder.Entity<WholesalerProduct>()
                .HasOne(wp => wp.Wholesaler)
                .WithMany()
                .HasForeignKey(wp => wp.WholesalerId)
                .OnDelete(DeleteBehavior.Restrict);

            // RetailerProduct - WholesalerProduct
            modelBuilder.Entity<RetailerProduct>()
                .HasOne(rp => rp.WholesalerProduct)
                .WithMany()
                .HasForeignKey(rp => rp.WholesalerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // RetailerProduct - Retailer (ApplicationUser)
            modelBuilder.Entity<RetailerProduct>()
                .HasOne(rp => rp.Retailer)
                .WithMany()
                .HasForeignKey(rp => rp.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Retailer (ApplicationUser)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Retailer)
                .WithMany()
                .HasForeignKey(o => o.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Wholesaler (ApplicationUser) - Optional for partnership orders
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Wholesaler)
                .WithMany()
                .HasForeignKey(o => o.WholesalerId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // OrderItem - Order
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderItem - WholesalerProduct - Optional for partnership orders
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.WholesalerProduct)
                .WithMany(wp => wp.OrderItems)
                .HasForeignKey(oi => oi.WholesalerProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            // Cart - Retailer (ApplicationUser)
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Retailer)
                .WithMany()
                .HasForeignKey(c => c.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // CartItem - Cart
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // CartItem - WholesalerProduct
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.WholesalerProduct)
                .WithMany(wp => wp.CartItems)
                .HasForeignKey(ci => ci.WholesalerProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Transaction - Order (Many-to-One) 
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ignore Order.Transaction navigation property to prevent OrderId1 shadow property
            modelBuilder.Entity<Order>()
                .Ignore(o => o.Transaction);

            // Payment - Transaction
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Transaction)
                .WithMany(t => t.Payments)
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // RetailerPartnership - Retailer
            modelBuilder.Entity<RetailerPartnership>()
                .HasOne(rp => rp.Retailer)
                .WithMany()
                .HasForeignKey(rp => rp.RetailerId)
                .OnDelete(DeleteBehavior.Restrict);

            // RetailerPartnerCategory - Partnership
            modelBuilder.Entity<RetailerPartnerCategory>()
                .HasOne(rpc => rpc.Partnership)
                .WithMany(rp => rp.Categories)
                .HasForeignKey(rpc => rpc.PartnershipId)
                .OnDelete(DeleteBehavior.Cascade);

            // RetailerPartnerProduct - Partnership
            modelBuilder.Entity<RetailerPartnerProduct>()
                .HasOne(rpp => rpp.Partnership)
                .WithMany(rp => rp.Products)
                .HasForeignKey(rpp => rpp.PartnershipId)
                .OnDelete(DeleteBehavior.Cascade);

            // RetailerPartnerProduct - Category
            modelBuilder.Entity<RetailerPartnerProduct>()
                .HasOne(rpp => rpp.Category)
                .WithMany(rpc => rpc.Products)
                .HasForeignKey(rpp => rpp.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
} 