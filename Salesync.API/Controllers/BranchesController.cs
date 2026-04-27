using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salesync.Domain.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ControllerBase
    {
        private readonly SalesyncDbContext _context;
        public BranchesController(SalesyncDbContext context)
        {
            _context = context;
        }

        // get all branches
        [HttpGet]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.Branches
                                         .Where(b=>b.IsActive==true)
                                         .ToListAsync();
            return Ok(branches);

        }

        // get branch by id
        [HttpGet("{id}")]
        public async Task<IActionResult> GetBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null && branch.IsActive== false)
            {
                return NotFound();
            }
            return Ok(branch);
        }

        // create a new branch
        [HttpPost]
        public async Task<IActionResult> CreateBranch([FromBody] Branch branch)
        {
            branch.CreatedAt = DateTime.UtcNow;
            branch.IsActive = true;

            await _context.Branches.AddAsync(branch);
            await _context.SaveChangesAsync();

            return Ok(branch);
        }

        // update an existing branch
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBranch(int id, [FromBody] Branch updatedBranch)
        {
            var existing = await _context.Branches.FindAsync(id);
            if (existing == null)
                return NotFound();

            existing.Name = updatedBranch.Name;
            existing.City = updatedBranch.City;
            existing.Address = updatedBranch.Address;
            existing.Phone = updatedBranch.Phone;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // delete a branch
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
                return NotFound();

            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
