namespace Salesync.Application.Modules.DataImport.Dtos.PriceLists
{
    public class PriceListImportRowDto
    {
        public int RowNumber { get; set; }

        public string Code { get; set; } =
            string.Empty;

        public string Name { get; set; } =
            string.Empty;

        public string? Description { get; set; }

        public string IsDefault { get; set; } =
            "false";

        public string? ValidFrom { get; set; }

        public string? ValidTo { get; set; }
    }
}