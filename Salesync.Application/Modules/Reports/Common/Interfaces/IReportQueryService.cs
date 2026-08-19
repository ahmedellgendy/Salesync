using Salesync.Application.Modules.Reports.Common.Models;

namespace Salesync.Application.Modules.Reports.Common.Interfaces
{
    public interface IReportQueryService
    {
        Task<decimal> GetGrossSalesAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<int> GetTotalInvoicesAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<decimal> GetTotalCollectionsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<decimal> GetTotalReturnsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<int> GetTotalVisitsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<int> GetCustomersVisitedAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SalesRepReportMetrics>> GetSalesRepMetricsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);
    }
}