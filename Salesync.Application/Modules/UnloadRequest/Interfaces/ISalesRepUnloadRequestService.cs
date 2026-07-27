using Salesync.Application.Modules.UnloadRequest.Dtos;

namespace Salesync.Application.Modules.UnloadRequest.Interfaces
{
    public interface ISalesRepUnloadRequestService
    {
        Task<SalesRepUnloadRequestDto> CreateAsync(CreateSalesRepUnloadRequestDto dto);

        Task<IEnumerable<SalesRepUnloadRequestDto>> GetMyRequestsAsync();

        Task<SalesRepUnloadRequestDto> GetByIdAsync(int id);

        Task<IEnumerable<SalesRepUnloadRequestDto>> GetPendingWarehouseRequestsAsync();

        Task<SalesRepUnloadRequestDto> ConfirmWarehouseAsync(int id,ConfirmSalesRepUnloadRequestDto dto);

        Task CancelAsync(int id, string? reason);
    }
}