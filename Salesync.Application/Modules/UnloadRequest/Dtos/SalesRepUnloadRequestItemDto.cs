namespace Salesync.Application.Modules.UnloadRequest.Dtos
{
    public class SalesRepUnloadRequestItemDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;

        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;

        public int RequestedLargeQuantity { get; set; }
        public int RequestedSmallQuantity { get; set; }
        public int RequestedQuantity { get; set; }

        public int ConfirmedLargeQuantity { get; set; }
        public int ConfirmedSmallQuantity { get; set; }
        public int ConfirmedQuantity { get; set; }

        public int VarianceQuantity { get; set; }

        public int SalesRepInventoryBeforeUnload { get; set; }
        public int SalesRepInventoryAfterUnload { get; set; }

        public string? SalesRepNotes { get; set; }
        public string? WarehouseNotes { get; set; }
    }
}