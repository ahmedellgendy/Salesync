namespace Salesync.Application.Modules.MasterData.PriceLists.Dtos;

public class UpdateProductPriceDto
{
    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
}