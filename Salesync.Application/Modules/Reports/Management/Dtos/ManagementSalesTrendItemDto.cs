namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementSalesTrendItemDto
    {
        public DateOnly Date { get; set; }

        public decimal GrossSales { get; set; }

        public decimal TotalReturns { get; set; }

        public decimal NetSales { get; set; }

        public int TotalInvoices { get; set; }
    }
}