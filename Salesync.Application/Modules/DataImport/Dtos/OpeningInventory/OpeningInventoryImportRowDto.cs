namespace Salesync.Application.Modules.DataImport.Dtos.OpeningInventory
{
    public class OpeningInventoryImportRowDto
    {
        public int RowNumber { get; set; }

        public string WarehouseCode { get; set; } = string.Empty;

        public string ItemCode { get; set; } = string.Empty;

        public string LargeQuantity { get; set; } = "0";

        public string SmallQuantity { get; set; } = "0";

        public string? Notes { get; set; }
    }
}