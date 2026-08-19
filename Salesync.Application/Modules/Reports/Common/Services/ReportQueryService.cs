using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;

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
    }
}