namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementBranchPerformanceItemDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalCollections { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }
        public int ActiveSalesReps { get; set; }
    }
}