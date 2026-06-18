using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.Infrastructure.Repositories.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SalesyncDbContext _context;

        public IGenericRepository<Branch> Branches { get; }
        public IGenericRepository<Warehouse> Warehouses { get; }
        public IGenericRepository<Product> Products { get; }
        public IGenericRepository<Customer> Customers { get; }

        public IGenericRepository<SalesRep> SalesReps { get; private set; }
        public IGenericRepository<Route> Routes { get; private set; }
        public IGenericRepository<RouteCustomer> RouteCustomers { get; private set; }

        public UnitOfWork(SalesyncDbContext context)
        {
            _context = context;

            Branches = new GenericRepository<Branch>(_context);
            Warehouses = new GenericRepository<Warehouse>(_context);
            Products = new GenericRepository<Product>(_context);
            Customers = new GenericRepository<Customer>(_context);

            SalesReps = new GenericRepository<SalesRep>(_context);
            Routes = new GenericRepository<Route>(_context);
            RouteCustomers = new GenericRepository<RouteCustomer>(_context);

        }


        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
    }
}
