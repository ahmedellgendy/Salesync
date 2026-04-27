using Salesync.Domain.Entities;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IGenericRepository<Branch> Branches { get; }
        IGenericRepository<Warehouse> Warehouses { get; }
        Task<int> CompleteAsync();
    }
}
