using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
        // MasterData
        IGenericRepository<Branch> Branches { get; }
        IGenericRepository<Warehouse> Warehouses { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Customer> Customers { get; }

        // SalesRep
        IGenericRepository<SalesRep> SalesReps { get; }
        IGenericRepository<Route> Routes { get; }
        IGenericRepository<RouteCustomer> RouteCustomers { get; }

        Task<int> CompleteAsync();
    }
}
