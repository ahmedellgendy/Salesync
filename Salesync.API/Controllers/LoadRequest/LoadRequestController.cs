using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.LoadRequest.Dtos;
using Salesync.Application.Modules.LoadRequest.Interfaces;

namespace Salesync.API.Controllers.LoadRequest
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoadRequestController : Controller
    {
        private readonly ILoadRequestService _loadRequestService;

        public LoadRequestController(ILoadRequestService loadRequestService)
        {
            _loadRequestService = loadRequestService;
        }

            
        [HttpGet] // GET: api/loadrequest
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _loadRequestService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<LoadRequestDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:int}")] // GET: api/loadrequest/1
        [Authorize(Roles = "Admin,Supervisor,Warehouse,SalesRep")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _loadRequestService.GetByIdAsync(id);
            return Ok(ApiResponse<LoadRequestDto>.SuccessResponse(result));
        }

        [HttpGet("salesrep/{salesRepId:int}")] // GET: api/loadrequest/salesrep/1
        [Authorize(Roles = "Admin,Supervisor,Warehouse,SalesRep")]
        public async Task<IActionResult> GetBySalesRepAsync(int salesRepId)
        {
            var result = await _loadRequestService.GetBySalesRepAsync(salesRepId);
            return Ok(ApiResponse<IEnumerable<LoadRequestDto>>.SuccessResponse(result));
        }

        [HttpPost] // POST: api/loadrequest
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateLoadRequestDto dto)
        {
            var result = await _loadRequestService.CreateAsync(dto);
            return Ok(ApiResponse<LoadRequestDto>.SuccessResponse(
                result,
                "Load request created successfully"));
        }

        [HttpPut("{id:int}/approve")] // PUT: api/loadrequest/1/approve
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> ApproveAsync(int id, [FromBody] ApproveLoadRequestDto dto)
        {
            var result = await _loadRequestService.ApproveAsync(id, dto);
            return Ok(ApiResponse<LoadRequestDto>.SuccessResponse(result, "Load request approved successfully"));
        }

        [HttpPut("{id:int}/reject")] // PUT: api/loadrequest/1/reject
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> RejectAsync(int id, [FromBody] RejectLoadRequestDto dto)
        {
            var result = await _loadRequestService.RejectAsync(id, dto);
            return Ok(ApiResponse<LoadRequestDto>.SuccessResponse(result, "Load request rejected successfully"));
        }

        [HttpPut("{id:int}/warehouse-confirm")] // PUT: api/loadrequest/1/warehouse-confirm
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> ConfirmWarehouseAsync(int id, [FromBody] ConfirmLoadRequestDto dto)
        {
            var result = await _loadRequestService.ConfirmWarehouseAsync(id, dto);
            return Ok(ApiResponse<LoadRequestDto>.SuccessResponse(result, "Load request warehouse confirmed successfully"));
        }

        [HttpPut("{id:int}/cancel")] // PUT: api/loadrequest/1/cancel
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CancelAsync(int id)
        {
            await _loadRequestService.CancelAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Load request cancelled successfully"));
        }
    }
}