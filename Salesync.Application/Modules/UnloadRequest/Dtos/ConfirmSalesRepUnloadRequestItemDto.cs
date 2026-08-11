namespace Salesync.Application.Modules.UnloadRequest.Dtos
{
    public class ConfirmSalesRepUnloadRequestItemDto
    {
        public int UnloadRequestItemId { get; set; }

        public int ConfirmedLargeQuantity { get; set; }
        public int ConfirmedSmallQuantity { get; set; }

        public string? WarehouseNotes { get; set; }
    }
}