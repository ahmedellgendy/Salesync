namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementSalesRepRankingItemDto
    {
        public int Rank { get; set; }

        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }
        public decimal TotalCollections { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }
        public int CustomersVisited { get; set; }

        public decimal AverageInvoiceValue { get; set; }
    }
}