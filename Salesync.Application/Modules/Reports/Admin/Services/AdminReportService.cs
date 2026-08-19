using Salesync.Application.Modules.Reports.Admin.Dtos;
using Salesync.Application.Modules.Reports.Admin.Interfaces;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Models;

namespace Salesync.Application.Modules.Reports.Admin.Services
{
    public class AdminReportService : IAdminReportService
    {
        private readonly IReportScopeService _reportScopeService;
        private readonly IReportQueryService _reportQueryService;

        public AdminReportService(
            IReportScopeService reportScopeService,
            IReportQueryService reportQueryService)
        {
            _reportScopeService = reportScopeService;
            _reportQueryService = reportQueryService;
        }

        public async Task<AdminSummaryReportDto> GetSummaryAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds = scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                if (!scope.CanAccessSalesRep(filter.SalesRepId.Value))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
                {
                    filter.SalesRepId.Value
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

            var netSales = grossSales - totalReturns;

            return new AdminSummaryReportDto
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

        public async Task<PagedResult<AdminSalesReportItemDto>> GetSalesAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            if (filter is null)
                throw new ArgumentNullException(nameof(filter));

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            if (filter.PageNumber <= 0)
                throw new ArgumentException(
                    "PageNumber must be greater than zero.");

            if (filter.PageSize <= 0 || filter.PageSize > 100)
                throw new ArgumentException(
                    "PageSize must be between 1 and 100.");

            var scope = await _reportScopeService
                .GetCurrentScopeAsync(cancellationToken);

            var salesRepIds = scope.AllowedSalesRepIds.ToList();

            if (filter.SalesRepId.HasValue)
            {
                if (!scope.CanAccessSalesRep(filter.SalesRepId.Value))
                {
                    throw new UnauthorizedAccessException(
                        "You are not allowed to access reports for this sales representative.");
                }

                salesRepIds = new List<int>
        {
            filter.SalesRepId.Value
        };
            }

            var fromDate = filter.FromDate
                .ToDateTime(TimeOnly.MinValue);

            var toDateExclusive = filter.ToDate
                .AddDays(1)
                .ToDateTime(TimeOnly.MinValue);

            return await _reportQueryService
                .GetConfirmedSalesAsync(
                    salesRepIds,
                    fromDate,
                    toDateExclusive,
                    filter.PageNumber,
                    filter.PageSize,
                    x => new AdminSalesReportItemDto
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
                    },
                    cancellationToken);
        }

        public async Task<PagedResult<AdminInvoiceReportItemDto>> GetInvoicesAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            var context = await PrepareContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            return await _reportQueryService.GetInvoicesAsync(
                context.SalesRepIds,
                context.FromDate,
                context.ToDateExclusive,
                filter.PageNumber,
                filter.PageSize,
                x => new AdminInvoiceReportItemDto
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

                    RemainingAmount =
                        x.TotalAmount - x.PaidAmount
                },
                cancellationToken);
        }

        public async Task<PagedResult<AdminPaymentReportItemDto>> GetPaymentsAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            var context = await PrepareContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            return await _reportQueryService.GetPaymentsAsync(
                context.SalesRepIds,
                context.FromDate,
                context.ToDateExclusive,
                filter.PageNumber,
                filter.PageSize,
                x => new AdminPaymentReportItemDto
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
                },
                cancellationToken);
        }

        public async Task<PagedResult<AdminReturnReportItemDto>> GetReturnsAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            var context = await PrepareContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            return await _reportQueryService.GetReturnsAsync(
                context.SalesRepIds,
                context.FromDate,
                context.ToDateExclusive,
                filter.PageNumber,
                filter.PageSize,
                x => new AdminReturnReportItemDto
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
                },
                cancellationToken);
        }

        public async Task<PagedResult<AdminVisitReportItemDto>> GetVisitsAsync(ReportFilter filter,CancellationToken cancellationToken = default)
        {
            var context = await PrepareContextAsync(
                filter,
                validatePagination: true,
                cancellationToken);

            return await _reportQueryService.GetVisitsAsync(
                context.SalesRepIds,
                context.FromDate,
                context.ToDateExclusive,
                filter.PageNumber,
                filter.PageSize,
                x => new AdminVisitReportItemDto
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
                },
                cancellationToken);
        }







        private sealed class AdminReportContext
        {
            public List<int> SalesRepIds { get; init; } = new();

            public DateTime FromDate { get; init; }

            public DateTime ToDateExclusive { get; init; }
        }
        private async Task<AdminReportContext> PrepareContextAsync(ReportFilter filter, bool validatePagination, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(filter);

            if (filter.FromDate == default)
                throw new ArgumentException("FromDate is required.");

            if (filter.ToDate == default)
                throw new ArgumentException("ToDate is required.");

            if (filter.FromDate > filter.ToDate)
                throw new ArgumentException(
                    "FromDate cannot be greater than ToDate.");

            if (validatePagination)
            {
                if (filter.PageNumber <= 0)
                    throw new ArgumentException(
                        "PageNumber must be greater than zero.");

                if (filter.PageSize <= 0 || filter.PageSize > 100)
                    throw new ArgumentException(
                        "PageSize must be between 1 and 100.");
            }

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

            return new AdminReportContext
            {
                SalesRepIds = salesRepIds,

                FromDate = filter.FromDate
                    .ToDateTime(TimeOnly.MinValue),

                ToDateExclusive = filter.ToDate
                    .AddDays(1)
                    .ToDateTime(TimeOnly.MinValue)
            };
        }

    }
}