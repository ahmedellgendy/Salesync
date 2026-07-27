using Salesync.Domain.Common;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Domain.Modules.Sales.Entities
{
    public class InvoiceItem : BaseEntity
    {
        public int InvoiceId { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ItemCode { get; set; }
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
        public string? Notes { get; set; }

        // Navigation Properties
        public Invoice Invoice { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
