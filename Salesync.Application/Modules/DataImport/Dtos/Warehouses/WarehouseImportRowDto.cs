namespace Salesync.Application.Modules.DataImport.Dtos.Warehouses
{
    public class WarehouseImportRowDto
    {
        public int RowNumber { get; set; }

        public string WarehouseCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string BranchCode { get; set; } = string.Empty;

        public string? Location { get; set; }

        public string WarehouseType { get; set; } = string.Empty;

        public string? Latitude { get; set; }

        public string? Longitude { get; set; }
    }
}