namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class LoadRequestItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public int RequestedQuantity { get; set; }
        public int ApprovedQuantity { get; set; }
        public int ConfirmedQuantity { get; set; }
        public string? Notes { get; set; }
    }
}