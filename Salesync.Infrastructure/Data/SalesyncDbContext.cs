using Microsoft.EntityFrameworkCore;
using Salesync.Domain.Entities;

namespace Salesync.Infrastructure.Data
{
    public class SalesyncDbContext : DbContext
    {
        public SalesyncDbContext(DbContextOptions<SalesyncDbContext> options) : base(options)
        {
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration for product
            modelBuilder.Entity<Product>()
                .Property(p => p.UnitPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Product>()
                .Property(p => p.CostPrice)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Product>()
                .Property(p => p.DiscountPercentage)
                .HasPrecision(18, 2);


            // Configuration for customer 
            modelBuilder.Entity<Customer>()
                .Property(c => c.CreditLimit)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Customer>()
                .Property(c => c.CurrentBalance)
                .HasPrecision(18, 2);
            modelBuilder.Entity<Customer>()
                .Property(c => c.TotalPurchaseAmount)
                .HasPrecision(18, 2);

        }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
    }
}
