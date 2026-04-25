using Salesync.Application.Interfaces.Repositories;
using Salesync.Infrastructure.Data;

namespace Salesync.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SalesyncDbContext _context;

        public UnitOfWork(SalesyncDbContext context) 
        {
            _context = context;
        }
        public async Task<int> CompleteAsync()=> await _context.SaveChangesAsync();
    }
}
