using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Data
{
    public class SalesyncDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public SalesyncDbContext(DbContextOptions<SalesyncDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesyncDbContext).Assembly);
        }

        public DbSet<Branch> Branches { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
    }
}
