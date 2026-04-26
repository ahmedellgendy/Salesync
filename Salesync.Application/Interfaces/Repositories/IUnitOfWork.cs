using Salesync.Domain.Entities;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        IGenericRepository<Branch> Branches { get; }
        Task<int> CompleteAsync();
    }
}
