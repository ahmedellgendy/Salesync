using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.UnloadRequest.Dtos;

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
        Task<InvoiceDto> GetInvoiceDetailsAsync(int invoiceId);

        Task<PaymentDto> CreatePaymentAsync(CreateSalesRepMobilePaymentDto dto);
        Task<InvoiceDto> CreateInvoiceAsync(CreateSalesRepMobileInvoiceDto dto);

        Task<IEnumerable<SalesRepMobileRouteDto>> GetRoutesAsync(int sessionId);
        Task<IEnumerable<SalesRepMobileCustomerDto>> GetRouteCustomersAsync(int sessionId, int routeId);

        Task<IEnumerable<MobileProductOptionDto>> GetProductsAsync();
        Task<IEnumerable<MobileWarehouseOptionDto>> GetWarehousesAsync();

        #region Invoice Returns

        Task<IEnumerable<InvoiceReturnDto>> GetMyReturnsAsync();

        Task<InvoiceReturnDto> GetReturnByIdAsync(int id);

        Task<InvoiceReturnDto> CreateReturnAsync(CreateInvoiceReturnDto dto);

        Task<InvoiceReturnDto> CancelReturnAsync(int id);

        #endregion

        #region Unload Requests

        Task<SalesRepUnloadRequestDto> CreateUnloadRequestAsync(CreateSalesRepUnloadRequestDto dto);

        Task<IEnumerable<SalesRepUnloadRequestDto>> GetMyUnloadRequestsAsync();

        Task<SalesRepUnloadRequestDto> GetUnloadRequestByIdAsync(int id);

        Task CancelUnloadRequestAsync(int id, string? reason);

        #endregion
    }
}