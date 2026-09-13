using Salesync.Domain.Common;

namespace Salesync.Domain.Modules.MasterData.Entities;

public class PriceList : BaseEntity
{
    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ProductPrice> ProductPrices { get; set; }
        = new List<ProductPrice>();
    public ICollection<Customer> Customers { get; set; }
         = new List<Customer>();
}