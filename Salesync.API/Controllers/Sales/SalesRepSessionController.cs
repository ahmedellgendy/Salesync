using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.Sales.Interfaces;

namespace Salesync.API.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesRepSessionController : ControllerBase
    {
        private readonly ISalesRepSessionService _sessionService;

        public SalesRepSessionController(ISalesRepSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _sessionService.GetByIdAsync(id);
            return Ok(ApiResponse<SalesRepSessionDto>.SuccessResponse(result));
        }

        [HttpGet("salesrep/{salesRepId}")]
        public async Task<IActionResult> GetBySalesRepAsync(int salesRepId)
        {
            var result = await _sessionService.GetBySalesRepAsync(salesRepId);
            return Ok(ApiResponse<IEnumerable<SalesRepSessionDto>>.SuccessResponse(result));
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveSessionsAsync()
        {
            var result = await _sessionService.GetActiveSessionsAsync();
            return Ok(ApiResponse<IEnumerable<SalesRepSessionDto>>.SuccessResponse(result));
        }

        [HttpGet("closed")]
        public async Task<IActionResult> GetClosedSessionsAsync()
        {
            var result = await _sessionService.GetClosedSessionsAsync();
            return Ok(ApiResponse<IEnumerable<SalesRepSessionDto>>.SuccessResponse(result));
        }

        [HttpPost("start")]
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> StartSessionAsync([FromBody] CreateSalesRepSessionDto dto)
        {
            var result = await _sessionService.StartSessionAsync(dto);
            return Ok(ApiResponse<SalesRepSessionDto>.SuccessResponse(result, "Session started successfully"));
        }

        [HttpPut("{id}/close")]
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CloseSessionAsync(int id)
        {
            var result = await _sessionService.CloseSessionAsync(id);
            return Ok(ApiResponse<SalesRepSessionDto>.SuccessResponse(result, "Session closed successfully"));
        }
    }
}
