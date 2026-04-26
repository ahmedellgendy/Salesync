using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SalesyncDbContext _context;

        public IGenericRepository<Branch> Branches { get; }
        public UnitOfWork(SalesyncDbContext context) 
        {
            _context = context;
            Branches = new GenericRepository<Branch>(_context);
        }


        public async Task<int> CompleteAsync()=> await _context.SaveChangesAsync();
    }
}
