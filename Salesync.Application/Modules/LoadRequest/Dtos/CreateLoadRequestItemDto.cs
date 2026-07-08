namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class CreateLoadRequestItemDto
    {
        public int ProductId { get; set; }
        public int RequestedQuantity { get; set; }
        public string? Notes { get; set; }
    }
}