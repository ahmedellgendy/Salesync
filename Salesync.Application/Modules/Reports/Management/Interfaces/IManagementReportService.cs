using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Application.Modules.Reports.Management.Dtos;

namespace Salesync.Application.Modules.Reports.Management.Interfaces
{
    public interface IManagementReportService
    {
        Task<ManagementSummaryReportDto> GetSummaryAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ManagementSalesTrendItemDto>> GetSalesTrendAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ManagementBranchPerformanceItemDto>> GetBranchPerformanceAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ManagementSalesRepRankingItemDto>> GetSalesRepRankingAsync(ReportFilter filter, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ManagementTopCustomerItemDto>>GetTopCustomersAsync(ReportFilter filter,CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ManagementTopProductItemDto>>GetTopProductsAsync(ReportFilter filter,CancellationToken cancellationToken = default);
    }
}