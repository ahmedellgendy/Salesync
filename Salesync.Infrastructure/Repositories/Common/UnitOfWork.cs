using Microsoft.EntityFrameworkCore.Storage;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Salesync.Domain.Modules.UnloadRequest.Entities;
using Salesync.Infrastructure.Data;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;

namespace Salesync.Infrastructure.Repositories.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SalesyncDbContext _context;
        private IDbContextTransaction? _transaction;

        public IGenericRepository<Branch> Branches { get; }
        public IGenericRepository<Warehouse> Warehouses { get; }
        public IGenericRepository<Product> Products { get; }
        public IGenericRepository<Customer> Customers { get; }

        public IGenericRepository<SalesRep> SalesReps { get; private set; }
        public IGenericRepository<Route> Routes { get; private set; }
        public IGenericRepository<RouteCustomer> RouteCustomers { get; private set; }

        public IGenericRepository<SalesRepSession> SalesRepSessions { get; private set; }
        public IGenericRepository<Invoice> Invoices { get; private set; }
        public IGenericRepository<InvoiceItem> InvoiceItems { get; private set; }
        public IGenericRepository<InvoiceReturn> InvoiceReturns { get; private set; }
        public IGenericRepository<InvoiceReturnItem> InvoiceReturnItems { get; private set; }
        public IGenericRepository<Payment> Payments { get; private set; }
        public IGenericRepository<SalesRepDayClosing> SalesRepDayClosings { get; private set; }
        public IGenericRepository<SalesRepDayClosingItem> SalesRepDayClosingItems { get; private set; }


        public IGenericRepository<StockBalance> StockBalances { get; }
        public IGenericRepository<StockMovement> StockMovements { get; }

        public IGenericRepository<LoadRequest> LoadRequests { get; private set; }
        public IGenericRepository<LoadRequestItem> LoadRequestItems { get; private set; }
        public IGenericRepository<SalesRepInventory> SalesRepInventories { get; private set; }
        public IGenericRepository<SalesRepInventoryMovement> SalesRepInventoryMovements { get; private set; }

        public IGenericRepository<CustomerVisitEntity> CustomerVisits { get; private set; }

        public IGenericRepository<CashBox> CashBoxes { get; }
        public IGenericRepository<CashReceipt> CashReceipts { get; }
        public IGenericRepository<SalesRepCashLedger> SalesRepCashLedgers { get; }
        public IGenericRepository<TreasuryTransaction> TreasuryTransactions { get; }
        public IGenericRepository<ExpenseCategory> ExpenseCategories { get; }
        public IGenericRepository<SalesRepUnloadRequest> SalesRepUnloadRequests { get; }
        public IGenericRepository<SalesRepUnloadRequestItem> SalesRepUnloadRequestItems { get; }

        public UnitOfWork(SalesyncDbContext context)
        {
            _context = context;

            Branches = new GenericRepository<Branch>(_context);
            Warehouses = new GenericRepository<Warehouse>(_context);
            Products = new GenericRepository<Product>(_context);
            Customers = new GenericRepository<Customer>(_context);

            SalesReps = new GenericRepository<SalesRep>(_context);
            Routes = new GenericRepository<Route>(_context);
            RouteCustomers = new GenericRepository<RouteCustomer>(_context);

            SalesRepSessions = new GenericRepository<SalesRepSession>(_context);
            Invoices = new GenericRepository<Invoice>(_context);
            InvoiceItems = new GenericRepository<InvoiceItem>(_context);
            InvoiceReturns = new GenericRepository<InvoiceReturn>(_context);
            InvoiceReturnItems = new GenericRepository<InvoiceReturnItem>(_context);
            Payments = new GenericRepository<Payment>(_context);
            SalesRepDayClosings = new GenericRepository<SalesRepDayClosing>(_context);
            SalesRepDayClosingItems = new GenericRepository<SalesRepDayClosingItem>(_context);

            StockBalances = new GenericRepository<StockBalance>(_context);
            StockMovements = new GenericRepository<StockMovement>(_context);

            LoadRequests = new GenericRepository<LoadRequest>(_context);
            LoadRequestItems = new GenericRepository<LoadRequestItem>(_context);
            SalesRepInventories = new GenericRepository<SalesRepInventory>(_context);
            SalesRepInventoryMovements = new GenericRepository<SalesRepInventoryMovement>(_context);

            CustomerVisits = new GenericRepository<CustomerVisitEntity>(_context);

            CashBoxes = new GenericRepository<CashBox>(_context);
            CashReceipts = new GenericRepository<CashReceipt>(_context);
            SalesRepCashLedgers = new GenericRepository<SalesRepCashLedger>(_context);
            TreasuryTransactions = new GenericRepository<TreasuryTransaction>(_context);
            ExpenseCategories = new GenericRepository<ExpenseCategory>(_context);

            SalesRepUnloadRequests = new GenericRepository<SalesRepUnloadRequest>(_context);
            SalesRepUnloadRequestItems = new GenericRepository<SalesRepUnloadRequestItem>(_context);
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                return;

            _transaction = await _context.Database.BeginTransactionAsync();
        }
        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                return;

            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                return;

            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
    }
}
