namespace Salesync.Application.Modules.Reports.Supervisor.Dtos
{
    public class SalesRepPerformanceReportItemDto
    {
        public int SalesRepId { get; set; }
        public string SalesRepName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalCollections { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }
        public int CustomersVisited { get; set; }

        public decimal AverageInvoiceValue { get; set; }
    }
}