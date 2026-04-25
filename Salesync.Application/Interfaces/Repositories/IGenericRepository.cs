using Salesync.Domain.Entities;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void UpdateAsync(T entity);
        void DeleteAsync(T entity);
    }
}
