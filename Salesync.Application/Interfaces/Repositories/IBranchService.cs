using Salesync.Application.Dtos;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllAsync();
        Task<BranchDto?> GetBranchByIdAsync(int id);
        Task AddBranchAsync(BranchDto branchDto);
        Task UpdateBranchAsync(BranchDto branchDto);
        Task DeleteBranchAsync(int id);
    }
}
