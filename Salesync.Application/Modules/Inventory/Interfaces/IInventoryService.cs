using Salesync.Application.Modules.Inventory.Dtos;
using Salesync.Domain.Common.Enums.Inventory;

namespace Salesync.Application.Modules.Inventory.Interfaces
{
    public interface IInventoryService
    {
        Task<StockBalanceDto> CreateOpeningBalanceAsync(CreateOpeningBalanceDto dto);
        Task<StockBalanceDto> GetBalanceAsync(int productId, int warehouseId);
        Task<IEnumerable<StockBalanceDto>> GetAllBalancesAsync();
        Task<IEnumerable<StockMovementDto>> GetMovementsAsync(int? productId = null, int? warehouseId = null);
        Task<StockMovementDto> StockInAsync(int productId, int warehouseId, int quantity, StockMovementSource source = StockMovementSource.StockAdjustment, int? sourceId = null, string? sourceNumber = null, string? notes = null);
        Task<StockMovementDto> StockOutAsync(int productId, int warehouseId, int quantity, StockMovementSource source = StockMovementSource.StockAdjustment, int? sourceId = null, string? sourceNumber = null, string? notes = null);
    }
}
