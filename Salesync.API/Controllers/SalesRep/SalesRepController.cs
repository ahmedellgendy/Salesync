using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepAccount;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;

namespace Salesync.API.Controllers.SalesRep
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesRepController : ControllerBase
    {
        private readonly ISalesRepService _salesRepService;
        private readonly ISalesRepAccountService _salesRepAccountService;

        public SalesRepController(ISalesRepService salesRepService, ISalesRepAccountService salesRepAccountService)
        {
            _salesRepService = salesRepService;
            _salesRepAccountService = salesRepAccountService;
        }

        [HttpGet] // GET: api/SalesRep
        public async Task<IActionResult> GetAllAsync()
        {
            var salesReps = await _salesRepService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<SalesRepDto>>.SuccessResponse(
            salesReps,
            "SalesReps retrieved successfully",
            200
            ));
        }

        [HttpGet("{id:int}")] // GET: api/SalesRep/{id}
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var salesRep = await _salesRepService.GetByIdAsync(id);
            return Ok(ApiResponse<SalesRepDto>.SuccessResponse(
                salesRep,
                "SalesRep retrieved successfully",
                200
            ));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateSalesRepWithAccountDto dto)
        {
            var result =await _salesRepAccountService.CreateAsync(dto);
            return Ok(ApiResponse<CreatedSalesRepWithAccountDto>.SuccessResponse(result, "Sales representative and login account created successfully."));
        }

        [HttpPut("{id:int}")] // PUT: api/SalesRep/{id}
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateSalesRepDto updateSalesRepDto)
        {
            var updatedSalesRep = await _salesRepService.UpdateAsync(id, updateSalesRepDto);
            return Ok(ApiResponse<SalesRepDto>.SuccessResponse(
                updatedSalesRep,
                "SalesRep updated successfully"
            ));
        }

        [HttpDelete("{id:int}")] // DELETE: api/SalesRep/{id}
        public async Task<IActionResult> DeleteAsync(int id)
        {
            await _salesRepService.DeleteAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse(
                null,
                "SalesRep deleted successfully"
            ));
        }
    }
}