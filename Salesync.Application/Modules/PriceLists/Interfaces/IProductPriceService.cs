using Salesync.Application.Modules.MasterData.PriceLists.Dtos;

namespace Salesync.Application.Modules.MasterData.PriceLists.Interfaces;

public interface IProductPriceService
{
    Task<IEnumerable<ProductPriceResponse>> GetAllAsync(
        int? priceListId = null,
        int? productId = null);

    Task<ProductPriceResponse?> GetByIdAsync(int id);

    Task<ProductPriceResponse> CreateAsync(
        CreateProductPriceDto dto);

    Task<ProductPriceResponse> UpdateAsync(
        int id,
        UpdateProductPriceDto dto);

    Task DeleteAsync(int id);
}