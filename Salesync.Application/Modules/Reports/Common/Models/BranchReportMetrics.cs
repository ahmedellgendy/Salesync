namespace Salesync.Application.Modules.Reports.Common.Models
{
    public class BranchReportMetrics
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalCollections { get; set; }
        public decimal TotalReturns { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }
        public int ActiveSalesReps { get; set; }
    }
}