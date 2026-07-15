using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Sales.Services;
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
            return Ok(ApiResponse<SalesRepDayClosingDto>.SuccessResponse(result, "Sales rep day closing retrieved successfully."));
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

        [HttpPut("{id}/receive-returned-stock")] // PUT: api/SalesRepDayClosing/5/receive-returned-stock
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> ReceiveReturnedStock(int id)
        {
            var result = await _dayClosingService.ReceiveReturnedStockAsync(id);
            return Ok(ApiResponse<SalesRepDayClosingDto>.SuccessResponse(result, "Returned stock received successfully."));
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