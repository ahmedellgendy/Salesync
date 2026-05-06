namespace Salesync.Application.Dtos.ProductDto
{
    public class CreateProductDto
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string SKU { get; set; }
        public required string Barcode { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; }
        public int MaxStockLevel { get; set; }
        public required string Unit { get; set; }
        public int? WarehouseId { get; set; }
    }
}
