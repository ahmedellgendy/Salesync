using Salesync.Application.Modules.LoadRequest.Dtos;

namespace Salesync.Application.Modules.LoadRequest.Interfaces
{
    public interface ILoadRequestService
    {
        Task<IEnumerable<LoadRequestDto>> GetAllAsync();

        Task<LoadRequestDto> GetByIdAsync(int id);

        Task<IEnumerable<LoadRequestDto>> GetBySalesRepAsync(int salesRepId);

        Task<IEnumerable<LoadRequestDto>> GetPendingAsync();

        Task<IEnumerable<LoadRequestDto>> GetApprovedAsync();

        Task<LoadRequestDto> CreateAsync(CreateLoadRequestDto dto);

        Task<LoadRequestDto> ApproveAsync(int id, ApproveLoadRequestDto dto);

        Task<LoadRequestDto> RejectAsync(int id, RejectLoadRequestDto dto);

        Task<LoadRequestDto> ConfirmWarehouseAsync(int id, ConfirmLoadRequestDto dto);

        Task CancelAsync(int id);

        Task<IEnumerable<SalesRepInventoryDto>> GetSalesRepInventoryAsync(int salesRepId);

        Task<IEnumerable<SalesRepInventoryMovementDto>> GetSalesRepInventoryMovementsAsync(int salesRepId, int? productId = null);


    }
}