namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class CreateLoadRequestDto
    {
        public int? SalesRepId { get; set; }
        public int WarehouseId { get; set; }
        public string? Notes { get; set; }

        public List<CreateLoadRequestItemDto> Items { get; set; } = new();
    }
}