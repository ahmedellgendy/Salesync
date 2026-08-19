using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Application.Modules.Reports.Supervisor.Dtos;

namespace Salesync.Application.Modules.Reports.Supervisor.Interfaces
{
    public interface ISupervisorReportService
    {
        Task<SupervisorSummaryReportDto> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<SupervisorSalesReportItemDto>> GetSalesAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<SupervisorInvoiceReportItemDto>> GetInvoicesAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<SupervisorPaymentReportItemDto>> GetPaymentsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<SupervisorReturnReportItemDto>> GetReturnsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<PagedResult<SupervisorVisitReportItemDto>> GetVisitsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SalesRepPerformanceReportItemDto>> GetSalesRepPerformanceAsync(ReportFilter filter, CancellationToken cancellationToken = default);
    }
}