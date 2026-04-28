namespace Salesync.Domain.Entities
{
    public class Product : BaseEntity
    {
        // Basic Info
        public required string Name { get; set; }
        public required string Description { get; set; }
        public required string Barcode { get; set; }
        
        // Price Info
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }

        // Stock Management
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; } 
        public int MaxStockLevel { get; set; } 
        public required string Unit { get; set; }

        // Relations
        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

    }
}
