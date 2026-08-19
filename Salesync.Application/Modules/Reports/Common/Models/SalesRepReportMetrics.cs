namespace Salesync.Application.Modules.Reports.Common.Models
{
    public class SalesRepReportMetrics
    {
        public int SalesRepId { get; set; }

        public decimal GrossSales { get; set; }
        public decimal TotalCollections { get; set; }
        public decimal TotalReturns { get; set; }

        public int TotalInvoices { get; set; }
        public int TotalVisits { get; set; }
        public int CustomersVisited { get; set; }
    }
}