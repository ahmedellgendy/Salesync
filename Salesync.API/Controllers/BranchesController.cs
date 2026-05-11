using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Dtos.BranchDto;
using Salesync.Application.Interfaces.Services;

namespace Salesync.API.Controllers
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

            if (branches == null || !branches.Any())
                return NoContent();

            return Ok(branches);
        }

        [HttpGet("{id}")] // GET: api/branches/{id} 
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _branchService.GetBranchByIdAsync(id);

            if (branch == null || !branch.IsActive)
                return NotFound(new { Message = $"Branch with ID {id} not found or inactive" });

            return Ok(branch);
        }

        [HttpPost] // POST: api/branches
        public async Task<IActionResult> CreateAsync([FromBody] CreateBranchDto createBranchDto)
        {
            var createdBranch = await _branchService.CreateBranchAsync(createBranchDto);
            return Ok(new
            {
                Message = $"Branch is created successfully",
                Branch = createdBranch
            });
        }

        [HttpPut("{id}")] // PUT: api/branches/{id}
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchDto updateBranchDto)
        {
            var updatedBranch = await _branchService.UpdateBranchAsync(id, updateBranchDto);
            return Ok(new
            {
                Message = $"Branch with ID {id} updated successfully",
                Branch = updatedBranch
            });
        }

        [HttpDelete("{id}")] // Delete: api/branches/{id}
        public async Task<IActionResult> DeleteBranch(int id)
        {
            await _branchService.DeleteBranchAsync(id);
            return Ok(new { Message = $"Branch with ID {id} deleted successfully" });
        }

    }
}
