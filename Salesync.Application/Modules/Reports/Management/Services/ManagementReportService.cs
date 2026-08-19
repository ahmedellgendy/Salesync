using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Application.Modules.Reports.Management.Dtos;
using Salesync.Application.Modules.Reports.Management.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Salesync.Application.Modules.Reports.Management.Services
{
    public class ManagementReportService : IManagementReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportScopeService _reportScopeService;
        private readonly IReportQueryService _reportQueryService;

        public ManagementReportService(
            IUnitOfWork unitOfWork,
            IReportScopeService reportScopeService,
            IReportQueryService reportQueryService)
        {
            _unitOfWork = unitOfWork;
            _reportScopeService = reportScopeService;
            _reportQueryService = reportQueryService;
        }

        public async Task<ManagementSummaryReportDto> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    salesRepId
                };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var grossSales = await _reportQueryService
                .GetGrossSalesAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var totalInvoices = await _reportQueryService
                .GetTotalInvoicesAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var totalCollections = await _reportQueryService
                .GetTotalCollectionsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var totalReturns = await _reportQueryService
                .GetTotalReturnsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var totalVisits = await _reportQueryService
                .GetTotalVisitsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var customersVisited = await _reportQueryService
                .GetCustomersVisitedAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var netSales =
                grossSales - totalReturns;

            return new ManagementSummaryReportDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                GrossSales = grossSales,
                NetSales = netSales,

                TotalCollections = totalCollections,
                TotalReturns = totalReturns,

                TotalInvoices = totalInvoices,
                TotalVisits = totalVisits,

                ActiveSalesReps = salesRepIds.Count,
                CustomersVisited = customersVisited
            };
        }

        public async Task<IReadOnlyCollection<ManagementSalesTrendItemDto>> GetSalesTrendAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    salesRepId
                };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var trend = await _reportQueryService
                .GetDailySalesTrendAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            return trend
                .Select(x => new ManagementSalesTrendItemDto
                {
                    Date = x.Date,
                    GrossSales = x.GrossSales,
                    TotalReturns = x.TotalReturns,
                    NetSales = x.GrossSales - x.TotalReturns,
                    TotalInvoices = x.TotalInvoices
                })
                .ToList();
        }

        public async Task<IReadOnlyCollection<ManagementBranchPerformanceItemDto>> GetBranchPerformanceAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    salesRepId
                };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var metrics = await _reportQueryService
                .GetBranchMetricsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            return metrics
                .Select(x => new ManagementBranchPerformanceItemDto
                {
                    BranchId = x.BranchId,
                    BranchName = x.BranchName,

                    GrossSales = x.GrossSales,
                    TotalCollections = x.TotalCollections,
                    TotalReturns = x.TotalReturns,
                    NetSales = x.GrossSales - x.TotalReturns,

                    TotalInvoices = x.TotalInvoices,
                    TotalVisits = x.TotalVisits,
                    ActiveSalesReps = x.ActiveSalesReps
                })
                .OrderByDescending(x => x.NetSales)
                .ToList();
        }

        public async Task<IReadOnlyCollection<ManagementSalesRepRankingItemDto>> GetSalesRepRankingAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    salesRepId
                };
            }


            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var salesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    salesRepIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.BranchId,
                    BranchName = x.Branch.Name
                })
                .ToListAsync(cancellationToken);

            var metrics = await _reportQueryService
                .GetSalesRepMetricsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var metricsByRep = metrics
                .ToDictionary(x => x.SalesRepId);

            var ordered = salesReps
                .Select(rep =>
                {
                    metricsByRep.TryGetValue(
                        rep.Id,
                        out var metric);

                    var grossSales =
                        metric?.GrossSales ?? 0m;

                    var totalReturns =
                        metric?.TotalReturns ?? 0m;

                    var totalInvoices =
                        metric?.TotalInvoices ?? 0;

                    return new ManagementSalesRepRankingItemDto
                    {
                        SalesRepId = rep.Id,
                        SalesRepName = rep.Name,

                        BranchId = rep.BranchId,
                        BranchName = rep.BranchName,

                        GrossSales = grossSales,
                        TotalReturns = totalReturns,
                        NetSales = grossSales - totalReturns,

                        TotalCollections =
                            metric?.TotalCollections ?? 0m,

                        TotalInvoices = totalInvoices,

                        TotalVisits =
                            metric?.TotalVisits ?? 0,

                        CustomersVisited =
                            metric?.CustomersVisited ?? 0,

                        AverageInvoiceValue =
                            totalInvoices == 0
                                ? 0m
                                : grossSales / totalInvoices
                    };
                })
                .OrderByDescending(x => x.NetSales)
                .ThenByDescending(x => x.TotalCollections)
                .ThenByDescending(x => x.TotalInvoices)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].Rank = i + 1;
            }

            return ordered;
        }

        public async Task<IReadOnlyCollection<ManagementTopCustomerItemDto>> GetTopCustomersAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    salesRepId
                };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var metrics = await _reportQueryService
                .GetCustomerMetricsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var topCustomers = metrics
                .Select(x =>
                {
                    var netSales =
                        x.GrossSales - x.TotalReturns;

                    return new ManagementTopCustomerItemDto
                    {
                        CustomerId = x.CustomerId,
                        CustomerName = x.CustomerName,

                        GrossSales = x.GrossSales,
                        TotalReturns = x.TotalReturns,
                        NetSales = netSales,

                        TotalCollections =
                            x.TotalCollections,

                        TotalInvoices =
                            x.TotalInvoices,

                        AverageInvoiceValue =
                            x.TotalInvoices == 0
                                ? 0m
                                : x.GrossSales / x.TotalInvoices
                    };
                })
                .OrderByDescending(x => x.NetSales)
                .ThenByDescending(x => x.TotalCollections)
                .Take(10)
                .ToList();

            for (var i = 0; i < topCustomers.Count; i++)
            {
                topCustomers[i].Rank = i + 1;
            }

            return topCustomers;
        }

        public async Task<IReadOnlyCollection<ManagementTopProductItemDto>>GetTopProductsAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var salesRepId = filter.SalesRepId.Value;

                if (!scope.CanAccessSalesRep(salesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                 {
            salesRepId
                 };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            var metrics = await _reportQueryService
                .GetProductMetricsAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    cancellationToken);

            var topProducts = metrics
                .Select(x => new ManagementTopProductItemDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    ItemCode = x.ItemCode,

                    SoldQuantity = x.SoldQuantity,
                    ReturnedQuantity = x.ReturnedQuantity,

                    GrossSales = x.GrossSales,
                    TotalReturns = x.TotalReturns,
                    NetSales = x.GrossSales - x.TotalReturns
                })
                .OrderByDescending(x => x.NetSales)
                .ThenByDescending(x => x.SoldQuantity)
                .Take(10)
                .ToList();

            for (var i = 0; i < topProducts.Count; i++)
            {
                topProducts[i].Rank = i + 1;
            }

            return topProducts;
        }
    }
}