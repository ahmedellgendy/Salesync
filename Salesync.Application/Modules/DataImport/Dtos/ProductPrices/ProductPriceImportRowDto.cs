namespace Salesync.Application.Modules.DataImport.Dtos.ProductPrices
{
    public class ProductPriceImportRowDto
    {
        public int RowNumber { get; set; }

        public string PriceListCode { get; set; } =
            string.Empty;

        public string ItemCode { get; set; } =
            string.Empty;

        public string UnitPrice { get; set; } =
            string.Empty;

        public string DiscountPercentage { get; set; } =
            "0";

        public string? ValidFrom { get; set; }

        public string? ValidTo { get; set; }
    }
}