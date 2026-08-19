using Salesync.Application.Modules.Reports.Common.Models;
using Salesync.Domain.Modules.Sales.Entities;
using System.Linq.Expressions;
using CustomerVisitEntity = Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit;

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

        Task<PagedResult<TDto>> GetConfirmedSalesAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<Salesync.Domain.Modules.Sales.Entities.Invoice, TDto>> selector,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TDto>> GetInvoicesAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
           DateTime fromDate,
           DateTime toDateExclusive,
           int pageNumber,
           int pageSize,
           Expression<Func<Invoice, TDto>> selector,
           CancellationToken cancellationToken = default);

        Task<PagedResult<TDto>> GetPaymentsAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<Payment, TDto>> selector,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TDto>> GetReturnsAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<InvoiceReturn, TDto>> selector,
            CancellationToken cancellationToken = default);

        Task<PagedResult<TDto>> GetVisitsAsync<TDto>(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            int pageNumber,
            int pageSize,
            Expression<Func<CustomerVisitEntity, TDto>> selector,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<SalesTrendMetrics>> GetDailySalesTrendAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<BranchReportMetrics>> GetBranchMetricsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<CustomerReportMetrics>> GetCustomerMetricsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<ProductReportMetrics>> GetProductMetricsAsync(
            IReadOnlyCollection<int> salesRepIds,
            DateTime fromDate,
            DateTime toDateExclusive,
            CancellationToken cancellationToken = default);
    }
}