namespace Salesync.Application.Modules.LoadRequest.Dtos
{
    public class SalesRepInventoryDto
    {
        public int Id { get; set; }
        public int SalesRepId { get; set; }
        public string? SalesRepName { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ItemCode { get; set; }
        public int Quantity { get; set; }

        public DateTime LastUpdatedAt { get; set; }
    }
}