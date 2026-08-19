namespace Salesync.Application.Modules.Reports.Common.Models
{
    public class SalesTrendMetrics
    {
        public DateOnly Date { get; set; }

        public decimal GrossSales { get; set; }

        public decimal TotalReturns { get; set; }

        public int TotalInvoices { get; set; }
    }
}