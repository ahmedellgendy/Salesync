using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.MasterData.Dtos.BranchDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;


namespace Salesync.API.Controllers.MasterData
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly IBranchService _branchService;
        public BranchesController(IBranchService branchService)
        {
            _branchService = branchService;
        }


        [HttpGet] // GET: api/branches 
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _branchService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<BranchDto>>.SuccessResponse(
            branches,
            "Branches retrieved successfully",
            200
        ));
        }

        [HttpGet("{id}")] // GET: api/branches/{id} 
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);

            if (branch == null || !branch.IsActive)
                return NotFound(ApiResponse<BranchDto>.NotFoundResponse($"Branch with ID {id} not found or inactive"));

            return Ok(ApiResponse<BranchDto>.SuccessResponse(
                branch,
                "Branch retrieved successfully",
                200
            ));
        }

        [HttpPost] // POST: api/branches
        public async Task<IActionResult> CreateAsync([FromBody] CreateBranchDto createBranchDto)
        {
            var createdBranch = await _branchService.CreateBranchAsync(createBranchDto);
            return CreatedAtAction(nameof(GetBranch), new { id = createdBranch.Id },
            ApiResponse<BranchDto>.SuccessResponse(
                createdBranch,
                "Branch created successfully",
                201
            ));
        }

        [HttpPut("{id}")] // PUT: api/branches/{id}
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto updateBranchDto)
        {
            var updatedBranch = await _branchService.UpdateBranchAsync(id, updateBranchDto);
            return Ok(ApiResponse<BranchDto>.SuccessResponse(
            updatedBranch,
            "Branch updated successfully",
            200
        ));
        }

        [HttpDelete("{id}")] // Delete: api/branches/{id}
        public async Task<IActionResult> Delete(int id)
        {
            await _branchService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(
                        null,
                        $"Branch with ID {id} deleted successfully",
                        200
                    ));
        }

    }
}
