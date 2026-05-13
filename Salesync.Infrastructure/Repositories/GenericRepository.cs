using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Entities;
using Salesync.Infrastructure.Data;
using System.Linq.Expressions;

namespace Salesync.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly SalesyncDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(SalesyncDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync() => await _context.Set<T>().ToListAsync();
        public async Task<T?> GetByIdAsync(int id) => await _context.Set<T>().FindAsync(id);
        public async Task CreateAsync(T entity) => await _context.Set<T>().AddAsync(entity);
        public void UpdateAsync(T entity) => _context.Set<T>().Update(entity);
        public void Delete(T entity) => _context.Set<T>().Remove(entity);

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)=> await _context.Set<T>().Where(predicate).ToListAsync();
        

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            return entity;
        }
    }
}
