using Salesync.Application.Dtos.WarehouseDto;

namespace Salesync.Application.Interfaces.Services
{
    public interface IWarehouseService
    {
        Task<IEnumerable<WarehouseDto>> GetAllAsync();
        Task<WarehouseDto?> GetByIdAsync(int id);
        Task<WarehouseDto> CreateAsync(CreateWarehouseDto warehouseDto);
        Task<WarehouseDto> UpdateAsync(int id, UpdateWarehouseDto warehouseDto);
        Task<bool> DeleteAsync(int id);
    }
}
