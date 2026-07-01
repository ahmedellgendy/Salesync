using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
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

        #region MasterData DbSets
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        #endregion

        #region SalesRep Dbsets
        public DbSet<SalesRep> SalesReps { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteCustomer> RouteCustomers { get; set; }
        public DbSet<SalesRepRoute> SalesRepRoutes { get; set; }

        #endregion

        #region Sales Dbsets

        public DbSet<SalesRepSession> SalesRepSessions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }
        public DbSet<InvoiceReturn> InvoiceReturns { get; set; }
        public DbSet<InvoiceReturnItem> InvoiceReturnItems { get; set; }
        public DbSet<Payment> Payments { get; set; }

        #endregion
    }
}
