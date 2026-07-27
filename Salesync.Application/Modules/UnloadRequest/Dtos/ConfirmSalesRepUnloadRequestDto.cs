namespace Salesync.Application.Modules.UnloadRequest.Dtos
{
    public class ConfirmSalesRepUnloadRequestDto
    {
        public string? WarehouseNotes { get; set; }

        public List<ConfirmSalesRepUnloadRequestItemDto> Items { get; set; } = new();
    }
}