namespace Salesync.Application.Modules.MasterData.PriceLists.Dtos;

public class ProductPriceResponse
{
    public int Id { get; set; }

    public int PriceListId { get; set; }

    public string PriceListCode { get; set; } = string.Empty;

    public string PriceListName { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public string ItemCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }
}