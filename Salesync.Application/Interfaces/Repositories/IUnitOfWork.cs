namespace Salesync.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {

        Task<int> CompleteAsync();
    }
}
