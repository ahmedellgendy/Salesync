using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;

namespace Salesync.API.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesRepDayClosingController : Controller
    {

        private readonly ISalesRepDayClosingService _dayClosingService;

        public SalesRepDayClosingController(ISalesRepDayClosingService dayClosingService)
        {
            _dayClosingService = dayClosingService;
        }

        [HttpGet] // GET: api/SalesRepDayClosing
        [Authorize(Roles = "Admin,Supervisor,Warehouse,SalesRep")]
        public async Task<IActionResult> GetAll([FromQuery] SalesRepDayClosingFilterDto filter)
        {
            var result = await _dayClosingService.GetAllAsync(filter);
            return Ok(ApiResponse<IEnumerable<SalesRepDayClosingDto>>.SuccessResponse(result, "Sales rep day closings retrieved successfully."));
        }

        [HttpGet("{id}")] // GET: api/SalesRepDayClosing/5
        [Authorize(Roles = "Admin,Supervisor,Warehouse,SalesRep")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _dayClosingService.GetByIdAsync(id);
            return Ok(ApiResponse<SalesRepDayClosingDto> .SuccessResponse(result, "Sales rep day closing retrieved successfully."));
        }

        [HttpGet("salesrep/{salesRepId}")] // GET: api/SalesRepDayClosing/salesrep/5
        [Authorize(Roles = "Admin,Supervisor,Warehouse,SalesRep")]
        public async Task<IActionResult> GetBySalesRep(int salesRepId)
        {
            var result = await _dayClosingService.GetBySalesRepAsync(salesRepId);
            return Ok(ApiResponse<IEnumerable<SalesRepDayClosingDto>>.SuccessResponse(result, "Sales rep day closings retrieved successfully."));
        }

        [HttpPost] // POST: api/SalesRepDayClosing
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> Create([FromBody] CreateSalesRepDayClosingDto dto)
        {
            var result = await _dayClosingService.CreateAsync(dto);
            return Ok(ApiResponse<SalesRepDayClosingDto>.SuccessResponse(result, "Sales rep day closing submitted successfully."));
        }

        [HttpPut("{id}/approve")] // PUT: api/SalesRepDayClosing/5/approve
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _dayClosingService.ApproveAsync(id);
            return Ok(ApiResponse<SalesRepDayClosingDto>.SuccessResponse(result, "Sales rep day closing approved successfully."));
        }

        [HttpPut("{id}/reject")] // PUT: api/SalesRepDayClosing/5/reject
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectSalesRepDayClosingDto dto)
        {
            var result = await _dayClosingService.RejectAsync(id, dto);
            return Ok(ApiResponse<SalesRepDayClosingDto>.SuccessResponse(result, "Sales rep day closing rejected successfully."));
        }

        [HttpPut("{id}/cancel")]  // PUT: api/SalesRepDayClosing/5/cancel
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _dayClosingService.CancelAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Cancelled", "Sales rep day closing cancelled successfully."));
        }
    }
}