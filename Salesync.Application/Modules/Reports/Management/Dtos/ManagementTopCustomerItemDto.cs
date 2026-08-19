namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementTopCustomerItemDto
    {
        public int Rank { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        public decimal GrossSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }

        public decimal TotalCollections { get; set; }

        public int TotalInvoices { get; set; }

        public decimal AverageInvoiceValue { get; set; }
    }
}