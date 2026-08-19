using Salesync.Application.Modules.Reports.Common.Scopes;

namespace Salesync.Application.Modules.Reports.Common.Interfaces
{
    public interface IReportScopeService
    {
        Task<ReportScope> GetCurrentScopeAsync(CancellationToken cancellationToken = default);
    }
}