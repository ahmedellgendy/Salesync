using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using System.Security.Claims;

namespace Salesync.API.Controllers.SalesRep
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RouteController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RouteController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        [HttpGet] // GET: api/routes
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _routeService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<RouteDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")] // GET: api/routes/{id}
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _routeService.GetByIdAsync(id);
            return Ok(ApiResponse<RouteDto>.SuccessResponse(result));
        }

        [Authorize(Roles = "Supervisor")]
        [HttpGet("by-salesrep/{salesRepId:int}")] // GET: api/routes/by-salesrep/{salesRepId}
        public async Task<IActionResult> GetBySalesRep(int salesRepId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var routes = await _routeService.GetBySalesRepAsync(salesRepId, userId!);

            return Ok(ApiResponse<IEnumerable<RouteDto>>.SuccessResponse(
                routes,
                "Sales rep routes retrieved successfully"));
        }

        [HttpPost] // POST: api/routes
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateRouteDto dto)
        {
            var result = await _routeService.CreateAsync(dto);
            return Ok(ApiResponse<RouteDto>.SuccessResponse(
                result,
                "Route created successfully"));
        }

        [HttpPut("{id}")] // PUT: api/routes/{id}
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateRouteDto dto)
        {
            var result = await _routeService.UpdateAsync(id, dto);
            return Ok(ApiResponse<RouteDto>.SuccessResponse(
                result,
                "Route updated successfully"));
        }

        [HttpDelete("{id}")] // DELETE: api/routes/{id}
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _routeService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(
                null,
                "Route deleted successfully"));
        }
    }
}
