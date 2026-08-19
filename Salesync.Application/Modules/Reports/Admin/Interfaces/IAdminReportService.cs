using Salesync.Application.Modules.Reports.Admin.Dtos;
using Salesync.Application.Modules.Reports.Common.Models;

namespace Salesync.Application.Modules.Reports.Admin.Interfaces
{
    public interface IAdminReportService
    {
        Task<AdminSummaryReportDto> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminSalesReportItemDto>> GetSalesAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminInvoiceReportItemDto>> GetInvoicesAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminPaymentReportItemDto>> GetPaymentsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminReturnReportItemDto>> GetReturnsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<AdminVisitReportItemDto>> GetVisitsAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    }
}