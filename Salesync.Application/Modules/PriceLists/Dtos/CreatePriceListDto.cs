namespace Salesync.Application.Modules.MasterData.PriceLists.Dtos;

public class CreatePriceListDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsDefault { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}