using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using System.Security.Claims;

namespace Salesync.API.Controllers.SalesRep
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RouteCustomerController : ControllerBase
    {
        private readonly IRouteCustomerService _routeCustomerService;

        public RouteCustomerController(IRouteCustomerService routeCustomerService)
        {
            _routeCustomerService = routeCustomerService;
        }

        [HttpGet("route/{routeId}")] // GET: api/RouteCustomer/route/{routeId}
        public async Task<IActionResult> GetByRouteIdAsync(int routeId)
        {
            var result = await _routeCustomerService.GetByRouteIdAsync(routeId);
            return Ok(ApiResponse<IEnumerable<RouteCustomerDto>>.SuccessResponse(result));
        }

        [Authorize(Roles = "Supervisor")]
        [HttpGet("route/{routeId:int}/customers")] // GET: api/RouteCustomer/route/{routeId}/customers
        public async Task<IActionResult> GetRouteCustomers(int routeId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _routeCustomerService.GetRouteCustomersAsync(routeId, userId!);

            return Ok(ApiResponse<IEnumerable<RouteCustomerDetailsDto>>
                    .SuccessResponse(
                        result,
                        "Route customers retrieved successfully"));
        }

        [HttpPost] // POST: api/RouteCustomer
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateRouteCustomerDto dto)
        {
            var result = await _routeCustomerService.CreateAsync(dto);
            return Ok(ApiResponse<RouteCustomerDto>.SuccessResponse(
                result, 
                "RouteCustomer created successfully"));
        }

        [HttpPut("{id}")] // PUT: api/RouteCustomer/{id}
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateRouteCustomerDto dto)
        {
            var result = await _routeCustomerService.UpdateAsync(id, dto);
            return Ok(ApiResponse<RouteCustomerDto>.SuccessResponse(
                result,
                "RouteCustomer updated successfully"));
        }

        [HttpDelete("{id}")] // DELETE: api/RouteCustomer/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _routeCustomerService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(
                null, 
                "RouteCustomer deleted successfully"));
        }
    }
}
