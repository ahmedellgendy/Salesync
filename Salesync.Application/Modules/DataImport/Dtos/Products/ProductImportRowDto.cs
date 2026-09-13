namespace Salesync.Application.Modules.DataImport.Dtos.Products
{
    public class ProductImportRowDto
    {
        public int RowNumber { get; set; }

        public string ItemCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? SKU { get; set; }

        public string? Barcode { get; set; }

        public string UnitPrice { get; set; } = string.Empty;

        public string CostPrice { get; set; } = string.Empty;

        public string? DiscountPercentage { get; set; }

        public string? Unit { get; set; }

        public string SmallUnit { get; set; } = "قطعة";

        public string LargeUnit { get; set; } = "كرتونة";

        public string UnitsPerLargeUnit { get; set; } = "1";

        public string MinStockLevel { get; set; } = "0";

        public string MaxStockLevel { get; set; } = "0";

        public string EnableReturn { get; set; } = "true";

        public string ReturnDamaged { get; set; } = "false";

        public string? ReturnPeriod { get; set; }

        public string? WarehouseCode { get; set; }
    }
}