namespace Salesync.Application.Modules.UnloadRequest.Dtos
{
    public class CreateSalesRepUnloadRequestDto
    {
        public int SalesRepSessionId { get; set; }
        public int WarehouseId { get; set; }
        public string? SalesRepNotes { get; set; }
    }
}