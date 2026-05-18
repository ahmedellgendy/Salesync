using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Modules.MasterData.Entities;
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

        public UnitOfWork(SalesyncDbContext context)
        {
            _context = context;
            Branches = new GenericRepository<Branch>(_context);
            Warehouses = new GenericRepository<Warehouse>(_context);
            Products = new GenericRepository<Product>(_context);
            Customers = new GenericRepository<Customer>(_context);
        }


        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
    }
}
