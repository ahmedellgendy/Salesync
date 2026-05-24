using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.MasterData.Entities
{
    public class Product : BaseEntity
    {
        // Basic Info
        public required string ItemCode { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public string? SKU { get; set; } 
        public string? Barcode { get; set; }
        
        // Price Info
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public string? Unit { get; set; }

        // Stock Management
        public int MinStockLevel { get; set; } 
        public int MaxStockLevel { get; set; } 

        // Return Settings
        public bool EnableReturn { get; set; } = true;
        public bool ReturnDamaged { get; set; } 
        public int? ReturnPeriod { get; set; } 

        // Navigation Properties
        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

    }
}
