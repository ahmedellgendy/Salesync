using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IUnitOfWork
    {
         
        #region MasterData 

        IGenericRepository<Branch> Branches { get; }
        IGenericRepository<Warehouse> Warehouses { get; }
        IGenericRepository<Product> Products { get; }
        IGenericRepository<Customer> Customers { get; }

        #endregion

        #region SalesRep

        IGenericRepository<SalesRep> SalesReps { get; }
        IGenericRepository<Route> Routes { get; }
        IGenericRepository<RouteCustomer> RouteCustomers { get; }

        #endregion

        #region Sales

        IGenericRepository<SalesRepSession> SalesRepSessions { get; }
        IGenericRepository<Invoice> Invoices { get; }
        IGenericRepository<InvoiceItem> InvoiceItems { get; }
        IGenericRepository<InvoiceReturn> InvoiceReturns { get; }
        IGenericRepository<InvoiceReturnItem> InvoiceReturnItems { get; }
        IGenericRepository<Payment> Payments { get; }

        #endregion

        #region Inventory

        IGenericRepository<StockBalance> StockBalances { get; }
        IGenericRepository<StockMovement> StockMovements { get; }

        #endregion

        #region LoadRequest

        IGenericRepository<LoadRequest> LoadRequests { get; }
        IGenericRepository<LoadRequestItem> LoadRequestItems { get; }
        IGenericRepository<SalesRepInventory> SalesRepInventories { get; }
        IGenericRepository<SalesRepInventoryMovement> SalesRepInventoryMovements { get; }

        #endregion

        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        Task<int> CompleteAsync();
    }
}
