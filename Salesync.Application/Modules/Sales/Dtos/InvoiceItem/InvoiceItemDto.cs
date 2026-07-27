namespace Salesync.Application.Modules.Sales.Dtos.InvoiceItem
{
    public class InvoiceItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ItemCode { get; set; } = null!;


        // Small unit quantities
        public int Quantity { get; set; }
        public int BonusQuantity { get; set; }


        // Large unit quantities
        public int SaleLargeQuantity { get; set; }
        public int BonusLargeQuantity { get; set; }


        // Unit snapshot
        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;

        public decimal UnitPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal NetAmount { get; set; }
    }
}