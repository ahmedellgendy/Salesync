using Salesync.Application.Modules.MasterData.PriceLists.Dtos;

namespace Salesync.Application.Modules.MasterData.PriceLists.Interfaces;

public interface IPriceListService
{
    Task<IEnumerable<PriceListResponse>> GetAllAsync();

    Task<PriceListResponse?> GetByIdAsync(int id);

    Task<PriceListResponse> CreateAsync(
        CreatePriceListDto dto);

    Task<PriceListResponse> UpdateAsync(
        int id,
        UpdatePriceListDto dto);

    Task DeleteAsync(int id);
}