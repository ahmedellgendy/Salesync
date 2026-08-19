namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementSummaryReportDto
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        public decimal GrossSales { get; set; }
        public decimal NetSales { get; set; }
        public decimal TotalCollections { get; set; }
        public decimal TotalReturns { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }

        public int ActiveSalesReps { get; set; }
        public int CustomersVisited { get; set; }
    }
}