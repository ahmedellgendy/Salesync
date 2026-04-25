namespace Salesync.Domain.Interfaces.Repositories
{
    public interface IUnitOfWork
    {

        Task<int> CompleteAsync();
    }
}
