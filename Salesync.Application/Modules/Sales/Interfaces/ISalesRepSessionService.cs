using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public interface ISalesRepSessionService
    {
        Task<SalesRepSessionDto> GetByIdAsync(int id);
        Task<IEnumerable<SalesRepSessionDto>> GetBySalesRepAsync(int SalesRepId);
        Task<SalesRepSessionDto> StartSessionAsync(CreateSalesRepSessionDto dto);
        Task<SalesRepSessionDto> CloseSessionAsync(int id);
        Task<IEnumerable<SalesRepSessionDto>> GetActiveSessionsAsync();
        Task<IEnumerable<SalesRepSessionDto>> GetClosedSessionsAsync();

    }
}
