using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.UnloadRequest.Dtos;
using Salesync.Application.Modules.UnloadRequest.Interfaces;

namespace Salesync.API.Controllers
{
    [Route("api/unload-requests")]
    [ApiController]
    [Authorize]
    public class UnloadRequestsController : Controller
    {
        private readonly ISalesRepUnloadRequestService _unloadRequestService;

        public UnloadRequestsController(ISalesRepUnloadRequestService unloadRequestService)
        {
            _unloadRequestService = unloadRequestService;
        }

        [HttpPost] // POST: api/unload-requests
        [Authorize(Roles = "SalesRep")]
        public async Task<IActionResult> Create([FromBody] CreateSalesRepUnloadRequestDto dto)
        {
            var result = await _unloadRequestService.CreateAsync(dto);

            return Ok(ApiResponse<SalesRepUnloadRequestDto>.SuccessResponse(
                result,
                "Unload request created successfully."));
        }

        [HttpGet("my")] // GET: api/unload-requests/my
        [Authorize(Roles = "SalesRep")]
        public async Task<IActionResult> GetMyRequests()
        {
            var result = await _unloadRequestService.GetMyRequestsAsync();

            return Ok(ApiResponse<IEnumerable<SalesRepUnloadRequestDto>>.SuccessResponse(
                result,
                "Unload requests retrieved successfully."));
        }

        [HttpGet("{id:int}")] // GET: api/unload-requests/{id}
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _unloadRequestService.GetByIdAsync(id);

            return Ok(ApiResponse<SalesRepUnloadRequestDto>.SuccessResponse(
                result,
                "Unload request retrieved successfully."));
        }

        [HttpGet("pending-warehouse")]  // GET: api/unload-requests/pending-warehouse
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> GetPendingWarehouseRequests()
        {
            var result = await _unloadRequestService.GetPendingWarehouseRequestsAsync();

            return Ok(ApiResponse<IEnumerable<SalesRepUnloadRequestDto>>.SuccessResponse(
                result,
                "Pending warehouse unload requests retrieved successfully."));
        }

        [HttpPut("{id:int}/confirm-warehouse")]  // PUT: api/unload-requests/{id}/confirm-warehouse
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> ConfirmWarehouse(int id,[FromBody] ConfirmSalesRepUnloadRequestDto dto)
        {
            var result = await _unloadRequestService.ConfirmWarehouseAsync(id, dto);

            return Ok(ApiResponse<SalesRepUnloadRequestDto>.SuccessResponse(
                result,
                "Unload request confirmed successfully."));
        }

        [HttpPut("{id:int}/cancel")] // PUT: api/unload-requests/{id}/cancel
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelUnloadRequestDto dto)
        {
            await _unloadRequestService.CancelAsync(id, dto.Reason);

            return Ok(ApiResponse<string>.SuccessResponse(
                "Cancelled",
                "Unload request cancelled successfully."));
        }
    }
}