using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.MasterData.Entities;

public class ProductPrice : BaseEntity
{
    public int PriceListId { get; set; }

    public int ProductId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal DiscountPercentage { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;


    public PriceList PriceList { get; set; } = null!;

    public Product Product { get; set; } = null!;
}