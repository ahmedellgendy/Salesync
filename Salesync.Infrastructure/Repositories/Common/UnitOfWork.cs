using Microsoft.EntityFrameworkCore.Storage;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Domain.Modules.Company.Entities;
using Salesync.Domain.Modules.DataImport.Entities;
using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Salesync.Domain.Modules.UnloadRequest.Entities;
using Salesync.Infrastructure.Data;
using CustomerVisitEntity =
    Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;

namespace Salesync.Infrastructure.Repositories.Common
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SalesyncDbContext _context;

        private IDbContextTransaction? _transaction;

        // =====================================================
        // COMPANY
        // =====================================================

        public IGenericRepository<CompanyProfile> CompanyProfiles { get; }


        // =====================================================
        // MASTER DATA
        // =====================================================

        public IGenericRepository<Branch> Branches { get; }

        public IGenericRepository<Warehouse> Warehouses { get; }

        public IGenericRepository<Product> Products { get; }

        public IGenericRepository<Customer> Customers { get; }


        // =====================================================
        // SALES REP
        // =====================================================

        public IGenericRepository<SalesRep> SalesReps { get; }

        public IGenericRepository<Route> Routes { get; }

        public IGenericRepository<RouteCustomer> RouteCustomers { get; }


        // =====================================================
        // SALES
        // =====================================================

        public IGenericRepository<SalesRepSession> SalesRepSessions { get; }

        public IGenericRepository<Invoice> Invoices { get; }

        public IGenericRepository<InvoiceItem> InvoiceItems { get; }

        public IGenericRepository<InvoiceReturn> InvoiceReturns { get; }

        public IGenericRepository<InvoiceReturnItem> InvoiceReturnItems { get; }

        public IGenericRepository<Payment> Payments { get; }

        public IGenericRepository<SalesRepDayClosing> SalesRepDayClosings { get; }

        public IGenericRepository<SalesRepDayClosingItem> SalesRepDayClosingItems { get; }


        // =====================================================
        // INVENTORY
        // =====================================================

        public IGenericRepository<StockBalance> StockBalances { get; }

        public IGenericRepository<StockMovement> StockMovements { get; }


        // =====================================================
        // LOAD REQUEST
        // =====================================================

        public IGenericRepository<LoadRequest> LoadRequests { get; }

        public IGenericRepository<LoadRequestItem> LoadRequestItems { get; }

        public IGenericRepository<SalesRepInventory> SalesRepInventories { get; }

        public IGenericRepository<SalesRepInventoryMovement> SalesRepInventoryMovements { get; }


        // =====================================================
        // CUSTOMER VISITS
        // =====================================================

        public IGenericRepository<CustomerVisitEntity> CustomerVisits { get; }


        // =====================================================
        // TREASURY
        // =====================================================

        public IGenericRepository<CashBox> CashBoxes { get; }

        public IGenericRepository<CashReceipt> CashReceipts { get; }

        public IGenericRepository<SalesRepCashLedger> SalesRepCashLedgers { get; }

        public IGenericRepository<TreasuryTransaction> TreasuryTransactions { get; }

        public IGenericRepository<ExpenseCategory> ExpenseCategories { get; }


        // =====================================================
        // UNLOAD REQUEST
        // =====================================================

        public IGenericRepository<SalesRepUnloadRequest> SalesRepUnloadRequests { get; }

        public IGenericRepository<SalesRepUnloadRequestItem> SalesRepUnloadRequestItems { get; }


        // =====================================================
        // DATA IMPORT
        // =====================================================

        public IGenericRepository<ImportBatch> ImportBatches { get; }

        public IGenericRepository<ImportBatchRow> ImportBatchRows { get; }


        // =====================================================
        // PRICE LIST
        // =====================================================

        public IGenericRepository<PriceList> PriceLists { get; }

        public IGenericRepository<ProductPrice> ProductPrices { get; }


        // =====================================================
        // CONSTRUCTOR
        // =====================================================

        public UnitOfWork(
            SalesyncDbContext context)
        {
            _context =
                context;


            // Company
            CompanyProfiles =
                new GenericRepository<CompanyProfile>(_context);

            // Master Data
            Branches =
                new GenericRepository<Branch>(_context);

            Warehouses =
                new GenericRepository<Warehouse>(_context);

            Products =
                new GenericRepository<Product>(_context);

            Customers =
                new GenericRepository<Customer>(_context);


            // Sales Rep
            SalesReps =
                new GenericRepository<SalesRep>(_context);

            Routes =
                new GenericRepository<Route>(_context);

            RouteCustomers =
                new GenericRepository<RouteCustomer>(_context);


            // Sales
            SalesRepSessions =
                new GenericRepository<SalesRepSession>(_context);

            Invoices =
                new GenericRepository<Invoice>(_context);

            InvoiceItems =
                new GenericRepository<InvoiceItem>(_context);

            InvoiceReturns =
                new GenericRepository<InvoiceReturn>(_context);

            InvoiceReturnItems =
                new GenericRepository<InvoiceReturnItem>(_context);

            Payments =
                new GenericRepository<Payment>(_context);

            SalesRepDayClosings =
                new GenericRepository<SalesRepDayClosing>(_context);

            SalesRepDayClosingItems =
                new GenericRepository<SalesRepDayClosingItem>(_context);


            // Inventory
            StockBalances =
                new GenericRepository<StockBalance>(_context);

            StockMovements =
                new GenericRepository<StockMovement>(_context);


            // Load Request
            LoadRequests =
                new GenericRepository<LoadRequest>(_context);

            LoadRequestItems =
                new GenericRepository<LoadRequestItem>(_context);

            SalesRepInventories =
                new GenericRepository<SalesRepInventory>(_context);

            SalesRepInventoryMovements =
                new GenericRepository<SalesRepInventoryMovement>(_context);


            // Customer Visits
            CustomerVisits =
                new GenericRepository<CustomerVisitEntity>(_context);


            // Treasury
            CashBoxes =
                new GenericRepository<CashBox>(_context);

            CashReceipts =
                new GenericRepository<CashReceipt>(_context);

            SalesRepCashLedgers =
                new GenericRepository<SalesRepCashLedger>(_context);

            TreasuryTransactions =
                new GenericRepository<TreasuryTransaction>(_context);

            ExpenseCategories =
                new GenericRepository<ExpenseCategory>(_context);


            // Unload Request
            SalesRepUnloadRequests =
                new GenericRepository<SalesRepUnloadRequest>(_context);

            SalesRepUnloadRequestItems =
                new GenericRepository<SalesRepUnloadRequestItem>(_context);


            // Data Import
            ImportBatches =
                new GenericRepository<ImportBatch>(_context);

            ImportBatchRows =
                new GenericRepository<ImportBatchRow>(_context);


            // Price Lists
            PriceLists =
                new GenericRepository<PriceList>(_context);

            ProductPrices =
                new GenericRepository<ProductPrice>(_context);
        }


        // =====================================================
        // TRANSACTIONS
        // =====================================================

        public async Task BeginTransactionAsync()
        {
            if (_transaction is not null)
            {
                return;
            }

            _transaction =
                await _context.Database
                    .BeginTransactionAsync();
        }


        public async Task CommitTransactionAsync()
        {
            if (_transaction is null)
            {
                return;
            }

            await _transaction
                .CommitAsync();

            await _transaction
                .DisposeAsync();

            _transaction =
                null;
        }


        public async Task RollbackTransactionAsync()
        {
            if (_transaction is null)
            {
                return;
            }

            try
            {
                await _transaction
                    .RollbackAsync();
            }
            finally
            {
                await _transaction
                    .DisposeAsync();

                _transaction =
                    null;
            }
        }


        // =====================================================
        // CHANGE TRACKER
        // =====================================================

        public void ClearTracking()
        {
            _context.ChangeTracker.Clear();
        }


        // =====================================================
        // SAVE
        // =====================================================

        public async Task<int> CompleteAsync()
        {
            return await _context
                .SaveChangesAsync();
        }
    }
}