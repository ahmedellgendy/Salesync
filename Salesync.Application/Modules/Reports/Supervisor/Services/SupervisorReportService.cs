using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Application.Modules.Reports.Supervisor.Dtos;
using Salesync.Application.Modules.Reports.Supervisor.Interfaces;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Reports.Supervisor.Services
{
    public class SupervisorReportService : ISupervisorReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IReportScopeService _reportScopeService;

        public SupervisorReportService(
            IUnitOfWork unitOfWork,
            IReportScopeService reportScopeService)
        {
            _unitOfWork = unitOfWork;
            _reportScopeService = reportScopeService;
        }

        public async Task<SupervisorSummaryReportDto> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: false,
                cancellationToken);


            // INVOICES
            var invoiceSummary = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalInvoices = g.Count(),
                    GrossSales = g.Sum(x => x.TotalAmount)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalInvoices =
                invoiceSummary?.TotalInvoices ?? 0;

            var grossSales =
                invoiceSummary?.GrossSales ?? 0m;


            // PAYMENTS / COLLECTIONS
            var paymentSummary = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= context.FromDate &&
                    x.PaymentDate < context.ToDateExclusive)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalCollections = g.Sum(x => x.Amount)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalCollections =
                paymentSummary?.TotalCollections ?? 0m;


            // RETURNS
            var returnSummary = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalReturns =
                returnSummary?.TotalReturns ?? 0m;


            // VISITS
            var visitSummary = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    context.SalesRepIds.Contains(x.SalesRepId) &&
                    x.Status == VisitStatus.Completed &&
                    x.VisitDate >= context.FromDate &&
                    x.VisitDate < context.ToDateExclusive)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalVisits = g.Count(),

                    CustomersVisited = g
                        .Select(x => x.CustomerId)
                        .Distinct()
                        .Count()
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalVisits =
                visitSummary?.TotalVisits ?? 0;

            var customersVisited =
                visitSummary?.CustomersVisited ?? 0;


            // CALCULATED KPIs
            var netSales =
                grossSales - totalReturns;

            return new SupervisorSummaryReportDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                GrossSales = grossSales,
                NetSales = netSales,

                TotalCollections = totalCollections,
                TotalReturns = totalReturns,

                TotalInvoices = totalInvoices,
                TotalVisits = totalVisits,

                ActiveSalesReps = context.SalesRepIds.Count,
                CustomersVisited = customersVisited
            };
        }

        public async Task<PagedResult<SupervisorSalesReportItemDto>> GetSalesAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            var query = _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new SupervisorSalesReportItemDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,

                    InvoiceDate = x.CreatedAt,

                    SalesRepId = x.SalesRepId!.Value,
                    SalesRepName = x.SalesRep != null
                        ? x.SalesRep.Name
                        : string.Empty,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,

                    SubTotal = x.SubTotal,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,
                    TotalAmount = x.TotalAmount,

                    PaymentStatus = x.PaymentStatus
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SupervisorSalesReportItemDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<SupervisorInvoiceReportItemDto>> GetInvoicesAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            var query = _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new SupervisorInvoiceReportItemDto
                {
                    InvoiceId = x.Id,
                    InvoiceNumber = x.InvoiceNumber,
                    InvoiceDate = x.CreatedAt,

                    SalesRepId = x.SalesRepId!.Value,
                    SalesRepName = x.SalesRep != null
                        ? x.SalesRep.Name
                        : string.Empty,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,

                    Status = x.Status,
                    PaymentStatus = x.PaymentStatus,

                    SubTotal = x.SubTotal,
                    DiscountAmount = x.DiscountAmount,
                    TaxAmount = x.TaxAmount,

                    TotalAmount = x.TotalAmount,
                    PaidAmount = x.PaidAmount,

                    RemainingAmount = x.TotalAmount - x.PaidAmount
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SupervisorInvoiceReportItemDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<SupervisorPaymentReportItemDto>> GetPaymentsAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            var query = _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.PaymentDate >= context.FromDate &&
                    x.PaymentDate < context.ToDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.PaymentDate)
                .ThenByDescending(x => x.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new SupervisorPaymentReportItemDto
                {
                    PaymentId = x.Id,
                    PaymentNumber = x.PaymentNumber,
                    PaymentDate = x.PaymentDate,

                    SalesRepId = x.SalesRepId!.Value,
                    SalesRepName = x.SalesRep != null
                        ? x.SalesRep.Name
                        : string.Empty,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,

                    InvoiceId = x.InvoiceId,
                    InvoiceNumber = x.Invoice.InvoiceNumber,

                    Amount = x.Amount,

                    PaymentMethod = x.PaymentMethod,
                    Status = x.Status,

                    CheckNumber = x.CheckNumber,
                    CheckDueDate = x.CheckDueDate,
                    BankName = x.BankName,
                    TransactionReference = x.TransactionReference
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SupervisorPaymentReportItemDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<SupervisorReturnReportItemDto>> GetReturnsAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            var query = _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new SupervisorReturnReportItemDto
                {
                    ReturnId = x.Id,
                    ReturnNumber = x.ReturnNumber,
                    ReturnDate = x.CreatedAt,

                    SalesRepId = x.SalesRepId!.Value,
                    SalesRepName = x.SalesRep != null
                        ? x.SalesRep.Name
                        : string.Empty,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,

                    InvoiceId = x.InvoiceId,
                    InvoiceNumber = x.Invoice.InvoiceNumber,

                    ReturnReason = x.ReturnReason,
                    Status = x.Status,

                    TotalAmount = x.TotalAmount,
                    ReasonNotes = x.ReasonNotes
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SupervisorReturnReportItemDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<PagedResult<SupervisorVisitReportItemDto>> GetVisitsAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            var query = _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    context.SalesRepIds.Contains(x.SalesRepId) &&
                    x.VisitDate >= context.FromDate &&
                    x.VisitDate < context.ToDateExclusive);

            var totalCount = await query
                .CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(x => x.VisitDate)
                .ThenByDescending(x => x.Id)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new SupervisorVisitReportItemDto
                {
                    VisitId = x.Id,

                    VisitDate = x.VisitDate,
                    EndTime = x.EndTime,

                    SalesRepId = x.SalesRepId,
                    SalesRepName = x.SalesRep.Name,

                    CustomerId = x.CustomerId,
                    CustomerName = x.Customer.Name,

                    RouteId = x.RouteId,

                    VisitType = x.VisitType,
                    Status = x.Status,

                    NegativeReason = x.NegativeReason,

                    InvoiceId = x.InvoiceId,
                    PaymentId = x.PaymentId,
                    InvoiceReturnId = x.InvoiceReturnId,

                    Notes = x.Notes
                })
                .ToListAsync(cancellationToken);

            return new PagedResult<SupervisorVisitReportItemDto>
            {
                Items = items,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IReadOnlyCollection<SalesRepPerformanceReportItemDto>> GetSalesRepPerformanceAsync(ReportFilter filter, CancellationToken cancellationToken = default)
        {
            var context = await PrepareReportContextAsync(
                filter,
                validatePagination: false,
                cancellationToken);


            // SALES REPS
            var salesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    context.SalesRepIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.Name
                })
                .ToListAsync(cancellationToken);


            // INVOICE STATS
            var invoiceStats = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    GrossSales = g.Sum(x => x.TotalAmount),
                    TotalInvoices = g.Count()
                })
                .ToListAsync(cancellationToken);


            // PAYMENT STATS
            var paymentStats = await _unitOfWork.Payments
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentDate >= context.FromDate &&
                    x.PaymentDate < context.ToDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    TotalCollections = g.Sum(x => x.Amount)
                })
                .ToListAsync(cancellationToken);


            // RETURN STATS
            var returnStats = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId.HasValue &&
                    context.SalesRepIds.Contains(x.SalesRepId.Value) &&
                    x.Status == ReturnStatus.Approved &&
                    x.CreatedAt >= context.FromDate &&
                    x.CreatedAt < context.ToDateExclusive)
                .GroupBy(x => x.SalesRepId!.Value)
                .Select(g => new
                {
                    SalesRepId = g.Key,
                    TotalReturns = g.Sum(x => x.TotalAmount)
                })
                .ToListAsync(cancellationToken);


            // VISIT STATS
            var visitStats = await _unitOfWork.CustomerVisits
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    context.SalesRepIds.Contains(x.SalesRepId) &&
                    x.Status == VisitStatus.Completed &&
                    x.VisitDate >= context.FromDate &&
                    x.VisitDate < context.ToDateExclusive)
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


            // LOOKUP DICTIONARIES
            var invoicesByRep = invoiceStats
                .ToDictionary(x => x.SalesRepId);

            var paymentsByRep = paymentStats
                .ToDictionary(x => x.SalesRepId);

            var returnsByRep = returnStats
                .ToDictionary(x => x.SalesRepId);

            var visitsByRep = visitStats
                .ToDictionary(x => x.SalesRepId);


            // FINAL RESULT
            var result = salesReps
                .Select(rep =>
                {
                    invoicesByRep.TryGetValue(
                        rep.Id,
                        out var invoice);

                    paymentsByRep.TryGetValue(
                        rep.Id,
                        out var payment);

                    returnsByRep.TryGetValue(
                        rep.Id,
                        out var returnStat);

                    visitsByRep.TryGetValue(
                        rep.Id,
                        out var visit);

                    var grossSales =
                        invoice?.GrossSales ?? 0m;

                    var totalInvoices =
                        invoice?.TotalInvoices ?? 0;

                    var totalCollections =
                        payment?.TotalCollections ?? 0m;

                    var totalReturns =
                        returnStat?.TotalReturns ?? 0m;

                    var totalVisits =
                        visit?.TotalVisits ?? 0;

                    var customersVisited =
                        visit?.CustomersVisited ?? 0;

                    var netSales =
                        grossSales - totalReturns;

                    var averageInvoiceValue =
                        totalInvoices == 0
                            ? 0m
                            : grossSales / totalInvoices;

                    return new SalesRepPerformanceReportItemDto
                    {
                        SalesRepId = rep.Id,
                        SalesRepName = rep.Name,

                        GrossSales = grossSales,
                        TotalCollections = totalCollections,
                        TotalReturns = totalReturns,
                        NetSales = netSales,

                        TotalInvoices = totalInvoices,
                        TotalVisits = totalVisits,
                        CustomersVisited = customersVisited,

                        AverageInvoiceValue = averageInvoiceValue
                    };
                })
                .OrderByDescending(x => x.NetSales)
                .ThenByDescending(x => x.TotalCollections)
                .ToList();

            return result;
        }

        #region Helper Methods

        private async Task<ReportExecutionContext> PrepareReportContextAsync(ReportFilter filter, bool validatePagination, CancellationToken cancellationToken)
        {
            ValidateFilter(
                filter,
                validatePagination);

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var effectiveSalesRepIds =
                scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                var requestedSalesRepId =
                    filter.SalesRepId.Value;

                if (!scope.AllowedSalesRepIds.Contains(
                        requestedSalesRepId))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                effectiveSalesRepIds = new List<int>
                {
                    requestedSalesRepId
                };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            return new ReportExecutionContext
            {
                SalesRepIds = effectiveSalesRepIds,
                FromDate = fromDate,
                ToDateExclusive = toDateExclusive
            };
        }

        private static void ValidateFilter(ReportFilter filter, bool validatePagination)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
            {
                throw new ArgumentException(
                    "FromDate is required.",
                    nameof(filter));
            }

            if (filter.ToDate == default)
            {
                throw new ArgumentException(
                    "ToDate is required.",
                    nameof(filter));
            }

            if (filter.FromDate > filter.ToDate)
            {
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.",
                    nameof(filter));
            }

            if (!validatePagination)
                return;

            if (filter.PageNumber <= 0)
            {
                throw new ArgumentException(
                    "PageNumber must be greater than zero.",
                    nameof(filter));
            }

            if (filter.PageSize <= 0)
            {
                throw new ArgumentException(
                    "PageSize must be greater than zero.",
                    nameof(filter));
            }

            if (filter.PageSize > 100)
            {
                throw new ArgumentException(
                    "PageSize cannot be greater than 100.",
                    nameof(filter));
            }
        }

        #endregion

        #region Helper Models

        private sealed class ReportExecutionContext
        {
            public List<int> SalesRepIds { get; init; } = new();

            public DateTime FromDate { get; init; }

            public DateTime ToDateExclusive { get; init; }
        }

        #endregion
    }
}