namespace Salesync.Application.Modules.MasterData.Dtos.ProductDto
{
    public class CreateProductDto
    {
        public required string ItemCode { get; set; }
        public required string Name { get; set; }

        public string? Description { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }

        public string? Unit { get; set; }

        public string SmallUnit { get; set; } = "قطعة";
        public string LargeUnit { get; set; } = "كرتونة";
        public int UnitsPerLargeUnit { get; set; } = 1;

        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }

        public bool EnableReturn { get; set; } = true;
        public bool ReturnDamaged { get; set; }
        public int? ReturnPeriod { get; set; }

        public int? WarehouseId { get; set; }
    }
}