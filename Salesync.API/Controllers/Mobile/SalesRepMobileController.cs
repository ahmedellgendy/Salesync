using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.UnloadRequest.Dtos;

namespace Salesync.API.Controllers.Mobile
{
    [Route("api/mobile/salesrep")]
    [ApiController]
    [Authorize(Roles = "SalesRep")]
    public class SalesRepMobileController : Controller
    {
        private readonly ISalesRepMobileService _salesRepMobileService;

        public SalesRepMobileController(ISalesRepMobileService salesRepMobileService)
        {
            _salesRepMobileService = salesRepMobileService;
        }

        [HttpGet("me")] // GET: api/mobile/me
        public async Task<IActionResult> GetProfile()
        {
            var result = await _salesRepMobileService.GetProfileAsync();
            return Ok(ApiResponse<SalesRepMobileProfileDto>.SuccessResponse(
                result,
                "Sales rep profile retrieved successfully."));
        }
        [HttpGet("today")] // GET: api/mobile/today
        public async Task<IActionResult> GetToday()
        {
            var result = await _salesRepMobileService.GetTodayAsync();
            return Ok(ApiResponse<SalesRepMobileTodayDto>.SuccessResponse(
                result,
                "Today session status retrieved successfully."));
        }

        [HttpPost("day/start")] // POST: api/mobile/day/start
        public async Task<IActionResult> StartDay([FromBody] StartSalesRepMobileDayDto dto)
        {
            var result = await _salesRepMobileService.StartDayAsync(dto);
            return Ok(ApiResponse<SalesRepSessionDto>.SuccessResponse(
                result,
                "Sales rep day started successfully."));
        }

        [HttpPut("day/{sessionId}/close")] // PUT: api/mobile/day/{sessionid}/close
        public async Task<IActionResult> CloseDay(int sessionId)
        {
            var result = await _salesRepMobileService.CloseDayAsync(sessionId);
            return Ok(ApiResponse<SalesRepSessionDto>.SuccessResponse(
                result,
                "Sales rep day closed successfully."));
        }

        [HttpGet("customers")] // GET: api/mobile/customers
        public async Task<IActionResult> GetCustomers([FromQuery] int sessionId)
        {
            var result = await _salesRepMobileService.GetCustomersAsync(sessionId);
            return Ok(ApiResponse<IEnumerable<SalesRepMobileCustomerDto>>.SuccessResponse(
                result,
                "Sales rep customers retrieved successfully."));
        }

        [HttpPost("visits/start")] // POST: api/mobile/visits/start
        public async Task<IActionResult> StartVisit([FromBody] StartSalesRepMobileVisitDto dto)
        {
            var result = await _salesRepMobileService.StartVisitAsync(dto);
            return Ok(ApiResponse<CustomerVisitDto>.SuccessResponse(
                result,
                "Customer visit started successfully."));
        }

        [HttpPut("visits/{visitId}/complete")] // PUT: api/mobile/visits/{visitid}/compete
        public async Task<IActionResult> CompleteVisit(int visitId, [FromBody] CompleteSalesRepMobileVisitDto dto)
        {
            var result = await _salesRepMobileService.CompleteVisitAsync(visitId, dto);
            return Ok(ApiResponse<CustomerVisitDto>.SuccessResponse(
                result,
                "Customer visit completed successfully."));
        }

        [HttpGet("invoices")] // GET /api/mobile/salesrep/invoices?sessionId=31 
        public async Task<IActionResult> GetInvoicesAsync([FromQuery] int sessionId)
        {
            var result = await _salesRepMobileService.GetInvoicesAsync(sessionId);
            return Ok(ApiResponse<IEnumerable<SalesRepMobileInvoiceDto>>.SuccessResponse(result));
        }

        [HttpPost("payments")] // POST: api/mobile/salesrep/payments
        public async Task<IActionResult> CreatePaymentAsync([FromBody] CreateSalesRepMobilePaymentDto dto)
        {
            var result = await _salesRepMobileService.CreatePaymentAsync(dto);

            return Ok(ApiResponse<PaymentDto>.SuccessResponse(
                result,
                "Payment created successfully."));
        }

        [HttpGet("routes")] // GET: api/mobile/salesrep/routes?sessionId=32
        public async Task<IActionResult> GetRoutes([FromQuery] int sessionId)
        {
            var result = await _salesRepMobileService.GetRoutesAsync(sessionId);
            return Ok(ApiResponse<IEnumerable<SalesRepMobileRouteDto>>.SuccessResponse(
                result,
                "Sales rep routes retrieved successfully."));
        }

        [HttpGet("routes/{routeId:int}/customers")] // GET: api/mobile/salesrep/routes/1/customers?sessionId=32
        public async Task<IActionResult> GetRouteCustomers(int routeId, [FromQuery] int sessionId)
        {
            var result = await _salesRepMobileService.GetRouteCustomersAsync(sessionId, routeId);
            return Ok(ApiResponse<IEnumerable<SalesRepMobileCustomerDto>>.SuccessResponse(
                result,
                "Route customers retrieved successfully."));
        }

        [HttpPost("invoices")] // GET: api/mobile/salesrep/invoices
        public async Task<IActionResult> CreateInvoiceAsync([FromBody] CreateSalesRepMobileInvoiceDto dto)
        {
            var result = await _salesRepMobileService.CreateInvoiceAsync(dto);
            return Ok(ApiResponse<InvoiceDto>.SuccessResponse(
                result,
                "Invoice created and visit completed successfully."));
        }

        [HttpGet("invoices/{invoiceId:int}")] // GET: api/mobile/salesrep/invoices/1
        public async Task<IActionResult> GetInvoiceDetailsAsync(int invoiceId)
        {
            var result = await _salesRepMobileService.GetInvoiceDetailsAsync(invoiceId);

            return Ok(ApiResponse<InvoiceDto>.SuccessResponse(
                result,
                "Invoice details retrieved successfully."));
        }

        [HttpGet("products")]  // GET: api/mobile/salesrep/products
        public async Task<IActionResult> GetProducts()
        {
            var result = await _salesRepMobileService.GetProductsAsync();

            return Ok(ApiResponse<IEnumerable<MobileProductOptionDto>>.SuccessResponse(
                result,
                "Products retrieved successfully."));
        }

        [HttpGet("warehouses")] // GET: api/mobile/salesrep/warehouses
        public async Task<IActionResult> GetWarehouses()
        {
            var result = await _salesRepMobileService.GetWarehousesAsync();

            return Ok(ApiResponse<IEnumerable<MobileWarehouseOptionDto>>.SuccessResponse(
                result,
                "Warehouses retrieved successfully."));
        }

        #region Invoice Returns

        [HttpGet("returns")]
        public async Task<IActionResult> GetMyReturnsAsync()
        {
            var result = await _salesRepMobileService.GetMyReturnsAsync();
            return Ok(ApiResponse<IEnumerable<InvoiceReturnDto>>.SuccessResponse(result));
        }

        [HttpGet("returns/{id:int}")]
        public async Task<IActionResult> GetReturnByIdAsync(int id)
        {
            var result = await _salesRepMobileService.GetReturnByIdAsync(id);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result));
        }

        [HttpPost("returns")]
        public async Task<IActionResult> CreateReturnAsync(
            [FromBody] CreateInvoiceReturnDto dto)
        {
            var result = await _salesRepMobileService.CreateReturnAsync(dto);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return created successfully"));
        }

        [HttpPut("returns/{id:int}/cancel")]
        public async Task<IActionResult> CancelReturnAsync(int id)
        {
            var result = await _salesRepMobileService.CancelReturnAsync(id);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return cancelled successfully"));
        }

        [HttpGet("customers/{customerId:int}/returnable-invoices")]
        public async Task<IActionResult> GetReturnableInvoices(int customerId)
        {
            var result = await _salesRepMobileService.GetReturnableInvoicesAsync(customerId);

            return Ok(ApiResponse<IEnumerable<MobileReturnableInvoiceDto>>.SuccessResponse(result, "Returnable invoices retrieved successfully."));
        }

        [HttpGet("returns/invoices/{invoiceId:int}")]
        public async Task<IActionResult> GetReturnableInvoiceDetails(int invoiceId)
        {
            var result = await _salesRepMobileService.GetReturnableInvoiceDetailsAsync(invoiceId);

            return Ok(ApiResponse<MobileReturnableInvoiceDetailsDto>.SuccessResponse(result, "Returnable invoice details retrieved successfully."));
        }
        #endregion

        #region Unload Requests

        [HttpPost("unload-requests")] // POST: api/mobile/salesrep/unload-requests
        public async Task<IActionResult> CreateUnloadRequest([FromBody] CreateSalesRepUnloadRequestDto dto)
        {
            var result = await _salesRepMobileService.CreateUnloadRequestAsync(dto);

            return Ok(ApiResponse<SalesRepUnloadRequestDto>.SuccessResponse(
                result,
                "Unload request created successfully."));
        }

        [HttpGet("unload-requests/my")] // GET: api/mobile/salesrep/unload-requests/my
        public async Task<IActionResult> GetMyUnloadRequests()
        {
            var result = await _salesRepMobileService.GetMyUnloadRequestsAsync();

            return Ok(ApiResponse<IEnumerable<SalesRepUnloadRequestDto>>.SuccessResponse(
                result,
                "Unload requests retrieved successfully."));
        }

        [HttpGet("unload-requests/{id:int}")]  // GET: api/mobile/salesrep/unload-requests/1
        public async Task<IActionResult> GetUnloadRequestById(int id)
        {
            var result = await _salesRepMobileService.GetUnloadRequestByIdAsync(id);

            return Ok(ApiResponse<SalesRepUnloadRequestDto>.SuccessResponse(
                result,
                "Unload request retrieved successfully."));
        }

        [HttpPut("unload-requests/{id:int}/cancel")]  // PUT: api/mobile/salesrep/unload-requests/1/cancel
        public async Task<IActionResult> CancelUnloadRequest(int id, [FromBody] CancelUnloadRequestDto? dto)
        {
            await _salesRepMobileService.CancelUnloadRequestAsync(id, dto?.Reason);

            return Ok(ApiResponse<string>.SuccessResponse(
                "Cancelled",
                "Unload request cancelled successfully."));
        }

        #endregion

    }
}