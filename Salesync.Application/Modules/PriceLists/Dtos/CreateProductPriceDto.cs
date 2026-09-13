namespace Salesync.Application.Modules.MasterData.PriceLists.Dtos;

public class CreateProductPriceDto
{
    public int PriceListId { get; set; }

    public int ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}