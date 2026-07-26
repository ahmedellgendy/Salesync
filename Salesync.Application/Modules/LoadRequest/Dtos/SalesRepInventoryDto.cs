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

        // الكمية محفوظة بالوحدة الصغرى
        public int Quantity { get; set; }
        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;
        public DateTime LastUpdatedAt { get; set; }
    }
}