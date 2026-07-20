using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;

namespace Salesync.Application.Modules.SalesRep.Interfaces.Services
{
    public interface ISalesRepMobileService
    {
        Task<SalesRepMobileProfileDto> GetProfileAsync();
        Task<SalesRepMobileTodayDto> GetTodayAsync();
        Task<IEnumerable<SalesRepMobileCustomerDto>> GetCustomersAsync(int sessionId);
        Task<SalesRepSessionDto> StartDayAsync(StartSalesRepMobileDayDto dto);
        Task<SalesRepSessionDto> CloseDayAsync(int sessionId);
    }
}