namespace Salesync.Application.Modules.Sales.Dtos.InvoiceReturn
{
    public class MobileReturnableInvoiceItemDto
    {
        public int InvoiceItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;


        // Original sold quantities
        public int SoldQuantity { get; set; }
        public int BonusQuantity { get; set; }


        // Already returned
        public int PreviouslyReturnedQuantity { get; set; }
        public int PreviouslyReturnedBonusQuantity { get; set; }


        // Still allowed to return
        public int RemainingReturnableQuantity { get; set; }
        public int RemainingReturnableBonusQuantity { get; set; }


        // Unit snapshot
        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }
}