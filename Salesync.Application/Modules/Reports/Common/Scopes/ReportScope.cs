namespace Salesync.Application.Modules.Reports.Common.Scopes
{
    public sealed class ReportScope
    {
        public IReadOnlyCollection<int> AllowedSalesRepIds { get; init; } = Array.Empty<int>();

        public IReadOnlyCollection<int> AllowedBranchIds { get; init; } = Array.Empty<int>();

        public IReadOnlyCollection<int> AllowedBusinessUnitIds { get; init; } = Array.Empty<int>();

        public bool CanAccessSalesRep(int salesRepId)
        {
            return AllowedSalesRepIds.Contains(salesRepId);
        }
    }
}