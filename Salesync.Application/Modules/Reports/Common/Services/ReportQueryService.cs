using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using System.Linq.Expressions;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;

namespace Salesync.Application.Modules.Reports.Common.Services
{
    public class ReportQueryService : IReportQueryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReportQueryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<decimal> GetGrossSalesAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .SumAsync(
                    x => (decimal?)x.TotalAmount,
                    cancellationToken)
                ?? 0m;
        }

        public async Task<int> GetTotalInvoicesAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.SalesRepId.HasValue &&
                        salesRepIds.Contains(x.SalesRepId.Value) &&
                        x.Status == InvoiceStatus.Confirmed &&
                        x.CreatedAt >= fromDate &&
                        x.CreatedAt < toDateExclusive,
                    cancellationToken);
        }

        public async Task<decimal> GetTotalCollectionsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= fromDate &&
                    x.PaymentDate < toDateExclusive)
                .SumAsync(
                    x => (decimal?)x.Amount,
                    cancellationToken)
                ?? 0m;
        }

        public async Task<decimal> GetTotalReturnsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .SumAsync(
                    x => (decimal?)x.TotalAmount,
                    cancellationToken)
                ?? 0m;
        }

        public async Task<int> GetTotalVisitsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .CountAsync(
                    x =>
                        salesRepIds.Contains(x.SalesRepId) &&
                        x.Status == VisitStatus.Completed &&
                        x.VisitDate >= fromDate &&
                        x.VisitDate < toDateExclusive,
                    cancellationToken);
        }

        public async Task<int> GetCustomersVisitedAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            return await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.SalesRepId) &&
                    x.Status == VisitStatus.Completed &&
                    x.VisitDate >= fromDate &&
                    x.VisitDate < toDateExclusive)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<SalesRepReportMetrics>> GetSalesRepMetricsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default)
        {
            var invoiceStats = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    GrossSales = g.Sum(x => x.TotalAmount),
                    TotalInvoices = g.Count()
                })
                .ToListAsync(cancellationToken);

            var paymentStats = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= fromDate &&
                    x.PaymentDate < toDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    TotalCollections = g.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            var returnStats = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var visitStats = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.SalesRepId) &&
                    x.Status == VisitStatus.Completed &&
                    x.VisitDate >= fromDate &&
                    x.VisitDate < toDateExclusive)
                .GroupBy(x => x.SalesRepId)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    TotalVisits = g.Count(),
                    CustomersVisited = g
                        .Select(x => x.CustomerId)
                        .Distinct()
                        .Count()
                })
                .ToListAsync(cancellationToken);

            var invoiceByRep = invoiceStats.ToDictionary(x => x.SalesRepId);

            var paymentByRep = paymentStats.ToDictionary(x => x.SalesRepId);

            var returnByRep = returnStats.ToDictionary(x => x.SalesRepId);

            var visitByRep = visitStats.ToDictionary(x => x.SalesRepId);

            var result = salesRepIds
                .Select(salesRepId =>
                {
                    invoiceByRep.TryGetValue(salesRepId, out var invoice);
                    paymentByRep.TryGetValue(salesRepId, out var payment);
                    returnByRep.TryGetValue(salesRepId, out var returnStat);
                    visitByRep.TryGetValue(salesRepId, out var visit);

                    return new SalesRepReportMetrics
                    {
                        SalesRepId = salesRepId,
                        GrossSales = invoice?.GrossSales ?? 0m,
                        TotalInvoices = invoice?.TotalInvoices ?? 0,
                        TotalCollections = payment?.TotalCollections ?? 0m,
                        TotalReturns = returnStat?.TotalReturns ?? 0m,
                        TotalVisits = visit?.TotalVisits ?? 0,
                        CustomersVisited = visit?.CustomersVisited ?? 0
                    };
                })
                .ToList();

            return result;
        }

        public async Task<PagedResult<TDto>> GetConfirmedSalesAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<Invoice, TDto>> selector,
            CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<TDto>> GetInvoicesAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<Invoice, TDto>> selector,
            CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<TDto>> GetPaymentsAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
             DateTime fromDate,
             DateTime toDateExclusive,
             int pageNumber,
             int pageSize,
             Expression<Func<Payment, TDto>> selector,
             CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.PaymentDate >= fromDate &&
                    x.PaymentDate < toDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<TDto>> GetReturnsAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
              DateTime fromDate,
              DateTime toDateExclusive,
              int pageNumber,
              int pageSize,
              Expression<Func<InvoiceReturn, TDto>> selector,
              CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<TDto>> GetVisitsAsync<TDto>(
              IReadOnlyCollection<int> salesRepIds,
              DateTime fromDate,
              DateTime toDateExclusive,
              int pageNumber,
              int pageSize,
              Expression<Func<CustomerVisitEntity, TDto>> selector,
              CancellationToken cancellationToken = default)
        {
            var query = _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.SalesRepId) &&
                    x.VisitDate >= fromDate &&
                    x.VisitDate < toDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.VisitDate)
                .ThenByDescending(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(selector)
                .ToListAsync(cancellationToken);

            return new PagedResult<TDto>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyCollection<SalesTrendMetrics>> GetDailySalesTrendAsync(
             IReadOnlyCollection<int> salesRepIds,
             DateTime fromDate,
             DateTime toDateExclusive,
             CancellationToken cancellationToken = default)
        {
            var invoiceStats = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    GrossSales = g.Sum(x => x.TotalAmount),
                    TotalInvoices = g.Count()
                })
                .ToListAsync(cancellationToken);

            var returnStats = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var invoiceByDate = invoiceStats
                .ToDictionary(x => x.Date.Date);

            var returnsByDate = returnStats
                .ToDictionary(x => x.Date.Date);

            var allDates = invoiceStats
                .Select(x => x.Date.Date)
                .Union(returnStats.Select(x => x.Date.Date))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var result = allDates
                .Select(date =>
                {
                    invoiceByDate.TryGetValue(date, out var invoice);
                    returnsByDate.TryGetValue(date, out var returnStat);

                    return new SalesTrendMetrics
                    {
                        Date = DateOnly.FromDateTime(date),

                        GrossSales =
                            invoice?.GrossSales ?? 0m,

                        TotalReturns =
                            returnStat?.TotalReturns ?? 0m,

                        TotalInvoices =
                            invoice?.TotalInvoices ?? 0
                    };
                })
                .ToList();

            return result;
        }

        public async Task<IReadOnlyCollection<BranchReportMetrics>> GetBranchMetricsAsync(
             IReadOnlyCollection<int> salesRepIds,
             DateTime fromDate,
             DateTime toDateExclusive,
             CancellationToken cancellationToken = default)
        {
            var salesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.BranchId,
                    BranchName = x.Branch.Name
                })
                .ToListAsync(cancellationToken);

            var invoiceStats = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.SalesRep!.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    GrossSales = g.Sum(x => x.TotalAmount),
                    TotalInvoices = g.Count()
                })
                .ToListAsync(cancellationToken);

            var paymentStats = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= fromDate &&
                    x.PaymentDate < toDateExclusive)
                .GroupBy(x => x.SalesRep!.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    TotalCollections = g.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            var returnStats = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.SalesRep!.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var visitStats = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.SalesRepId) &&
                    x.Status == VisitStatus.Completed &&
                    x.VisitDate >= fromDate &&
                    x.VisitDate < toDateExclusive)
                .GroupBy(x => x.SalesRep.BranchId)
                .Select(g => new
                {
                    BranchId = g.Key,
                    TotalVisits = g.Count()
                })
                .ToListAsync(cancellationToken);

            var salesRepStats = salesReps
                .GroupBy(x => new
                {
                    x.BranchId,
                    x.BranchName
                })
                .Select(g => new
                {
                    g.Key.BranchId,
                    g.Key.BranchName,
                    ActiveSalesReps = g.Count()
                })
                .ToList();

            var invoiceByBranch =
                invoiceStats.ToDictionary(x => x.BranchId);

            var paymentByBranch =
                paymentStats.ToDictionary(x => x.BranchId);

            var returnByBranch =
                returnStats.ToDictionary(x => x.BranchId);

            var visitByBranch =
                visitStats.ToDictionary(x => x.BranchId);

            var result = salesRepStats
                .Select(branch =>
                {
                    invoiceByBranch.TryGetValue(
                        branch.BranchId,
                        out var invoice);

                    paymentByBranch.TryGetValue(
                        branch.BranchId,
                        out var payment);

                    returnByBranch.TryGetValue(
                        branch.BranchId,
                        out var returnStat);

                    visitByBranch.TryGetValue(
                        branch.BranchId,
                        out var visit);

                    return new BranchReportMetrics
                    {
                        BranchId = branch.BranchId,
                        BranchName = branch.BranchName,

                        GrossSales =
                            invoice?.GrossSales ?? 0m,

                        TotalCollections =
                            payment?.TotalCollections ?? 0m,

                        TotalReturns =
                            returnStat?.TotalReturns ?? 0m,

                        TotalInvoices =
                            invoice?.TotalInvoices ?? 0,

                        TotalVisits =
                            visit?.TotalVisits ?? 0,

                        ActiveSalesReps =
                            branch.ActiveSalesReps
                    };
                })
                .OrderByDescending(x => x.GrossSales - x.TotalReturns)
                .ToList();

            return result;
        }

        public async Task<IReadOnlyCollection<CustomerReportMetrics>> GetCustomerMetricsAsync(
             IReadOnlyCollection<int> salesRepIds,
             DateTime fromDate,
             DateTime toDateExclusive,
             CancellationToken cancellationToken = default)
        {
            var invoiceStats = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => new
                {
                    x.CustomerId,
                    CustomerName = x.Customer.Name
                })
                .Select(g => new
                {
                    g.Key.CustomerId,
                    g.Key.CustomerName,
                    GrossSales = g.Sum(x => x.TotalAmount),
                    TotalInvoices = g.Count()
                })
                .ToListAsync(cancellationToken);

            var returnStats = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= fromDate &&
                    x.CreatedAt < toDateExclusive)
                .GroupBy(x => x.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var paymentStats = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= fromDate &&
                    x.PaymentDate < toDateExclusive)
                .GroupBy(x => x.CustomerId)
                .Select(g => new
                {
                    CustomerId = g.Key,
                    TotalCollections = g.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);

            var returnsByCustomer = returnStats
                .ToDictionary(x => x.CustomerId);

            var paymentsByCustomer = paymentStats
                .ToDictionary(x => x.CustomerId);

            var result = invoiceStats
                .Select(customer =>
                {
                    returnsByCustomer.TryGetValue(
                        customer.CustomerId,
                        out var returnStat);

                    paymentsByCustomer.TryGetValue(
                        customer.CustomerId,
                        out var paymentStat);

                    return new CustomerReportMetrics
                    {
                        CustomerId = customer.CustomerId,
                        CustomerName = customer.CustomerName,

                        GrossSales = customer.GrossSales,

                        TotalReturns =
                            returnStat?.TotalReturns ?? 0m,

                        TotalCollections =
                            paymentStat?.TotalCollections ?? 0m,

                        TotalInvoices =
                            customer.TotalInvoices
                    };
                })
                .OrderByDescending(x =>
                    x.GrossSales - x.TotalReturns)
                .ToList();

            return result;
        }

        public async Task<IReadOnlyCollection<ProductReportMetrics>> GetProductMetricsAsync(
             IReadOnlyCollection<int> salesRepIds,
             DateTime fromDate,
             DateTime toDateExclusive,
             CancellationToken cancellationToken = default)
        {
            var salesStats = await _unitOfWork.InvoiceItems
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.Invoice.SalesRepId.HasValue &&
                    salesRepIds.Contains(x.Invoice.SalesRepId.Value) &&
                    x.Invoice.Status == InvoiceStatus.Confirmed &&
                    x.Invoice.CreatedAt >= fromDate &&
                    x.Invoice.CreatedAt < toDateExclusive)
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.ProductName,
                    x.ItemCode
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.ItemCode,

                    SoldQuantity = g.Sum(x => x.Quantity),

                    GrossSales = g.Sum(x =>
                        x.NetAmount + x.TaxAmount)
                })
                .ToListAsync(cancellationToken);

            var returnStats = await _unitOfWork.InvoiceReturnItems
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.InvoiceReturn.SalesRepId.HasValue &&
                    salesRepIds.Contains(
                        x.InvoiceReturn.SalesRepId.Value) &&
                    x.InvoiceReturn.Status == ReturnStatus.Approved &&
                    x.InvoiceReturn.CreatedAt >= fromDate &&
                    x.InvoiceReturn.CreatedAt < toDateExclusive)
                .GroupBy(x => new
                {
                    x.ProductId,
                    x.ProductName,
                    x.ItemCode
                })
                .Select(g => new
                {
                    g.Key.ProductId,
                    g.Key.ProductName,
                    g.Key.ItemCode,

                    ReturnedQuantity =
                        g.Sum(x => x.Quantity),

                    TotalReturns =
                        g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);

            var salesByProduct = salesStats
                .ToDictionary(x => x.ProductId);

            var returnsByProduct = returnStats
                .ToDictionary(x => x.ProductId);

            var productIds = salesStats
                .Select(x => x.ProductId)
                .Union(returnStats.Select(x => x.ProductId))
                .Distinct()
                .ToList();

            var result = productIds
                .Select(productId =>
                {
                    salesByProduct.TryGetValue(
                        productId,
                        out var sale);

                    returnsByProduct.TryGetValue(
                        productId,
                        out var returnStat);

                    return new ProductReportMetrics
                    {
                        ProductId = productId,

                        ProductName =
                            sale?.ProductName ??
                            returnStat?.ProductName ??
                            string.Empty,

                        ItemCode =
                            sale?.ItemCode ??
                            returnStat?.ItemCode ??
                            string.Empty,

                        SoldQuantity =
                            sale?.SoldQuantity ?? 0,

                        ReturnedQuantity =
                            returnStat?.ReturnedQuantity ?? 0,

                        GrossSales =
                            sale?.GrossSales ?? 0m,

                        TotalReturns =
                            returnStat?.TotalReturns ?? 0m
                    };
                })
                .OrderByDescending(x =>
                    x.GrossSales - x.TotalReturns)
                .ToList();

            return result;
        }
    }
}