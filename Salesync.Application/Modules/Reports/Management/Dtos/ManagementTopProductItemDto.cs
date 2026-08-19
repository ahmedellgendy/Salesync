namespace Salesync.Application.Modules.Reports.Management.Dtos
{
    public class ManagementTopProductItemDto
    {
        public int Rank { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;

        public int SoldQuantity { get; set; }
        public int ReturnedQuantity { get; set; }

        public decimal GrossSales { get; set; }
        public decimal TotalReturns { get; set; }
        public decimal NetSales { get; set; }
    }
}