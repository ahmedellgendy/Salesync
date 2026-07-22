using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.Sales.Dtos.Payment;
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
        Task<CustomerVisitDto> StartVisitAsync(StartSalesRepMobileVisitDto dto);
        Task<CustomerVisitDto> CompleteVisitAsync(int visitId, CompleteSalesRepMobileVisitDto dto);
        Task<IEnumerable<SalesRepMobileInvoiceDto>> GetInvoicesAsync(int sessionId);
        Task<PaymentDto> CreatePaymentAsync(CreateSalesRepMobilePaymentDto dto);
        Task<IEnumerable<SalesRepMobileRouteDto>> GetRoutesAsync(int sessionId);
        Task<IEnumerable<SalesRepMobileCustomerDto>> GetRouteCustomersAsync(int sessionId, int routeId);
    }
}