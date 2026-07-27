using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.UnloadRequest.Entities
{
    public class SalesRepUnloadRequestItem : BaseEntity
    {
        public int SalesRepUnloadRequestId { get; set; }
        public SalesRepUnloadRequest SalesRepUnloadRequest { get; set; } = null!;

        public int ProductId { get; set; }

        // Product snapshot
        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;

        // Unit snapshot
        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;

        // Sales rep requested unload quantities
        public int RequestedLargeQuantity { get; set; }   // بالكرتونة
        public int RequestedSmallQuantity { get; set; }   // قطع متبقية
        public int RequestedQuantity { get; set; }        // بالقطعة

        // Warehouse confirmed received quantities
        public int ConfirmedLargeQuantity { get; set; }   // بالكرتونة
        public int ConfirmedSmallQuantity { get; set; }   // قطع متبقية
        public int ConfirmedQuantity { get; set; }        // بالقطعة

        // Difference between requested and confirmed
        public int VarianceQuantity { get; set; }         // بالقطعة

        // Snapshot before unload
        public int SalesRepInventoryBeforeUnload { get; set; }

        // Snapshot after warehouse confirmation
        public int SalesRepInventoryAfterUnload { get; set; }

        // Optional notes
        public string? SalesRepNotes { get; set; }
        public string? WarehouseNotes { get; set; }
    }
}