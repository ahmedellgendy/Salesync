using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly SalesyncDbContext _context;

        public GenericRepository(SalesyncDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();
        public async Task<T?> GetByIdAsync(int id) => await _context.Set<T>().FindAsync(id);
        public async Task AddAsync(T entity) => await _context.Set<T>().AddAsync(entity);
        public void UpdateAsync(T entity) => _context.Set<T>().Update(entity);
        public void DeleteAsync(T entity) => _context.Set<T>().Remove(entity);

        
    }
}
