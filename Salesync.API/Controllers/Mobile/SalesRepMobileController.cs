using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;

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
        public async Task<IActionResult> CompleteVisit(int visitId,[FromBody] CompleteSalesRepMobileVisitDto dto)
        {
            var result = await _salesRepMobileService.CompleteVisitAsync(visitId, dto);
            return Ok(ApiResponse<CustomerVisitDto>.SuccessResponse(
                result,
                "Customer visit completed successfully."));
        }
    }
}