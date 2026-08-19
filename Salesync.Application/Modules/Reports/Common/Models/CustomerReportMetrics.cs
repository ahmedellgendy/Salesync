namespace Salesync.Application.Modules.Reports.Common.Models
{
    public class CustomerReportMetrics
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal TotalCollections { get; set; }

        public int TotalInvoices { get; set; }
    }
}