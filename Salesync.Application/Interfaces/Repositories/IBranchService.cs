using Salesync.Application.Dtos.BranchDto;

namespace Salesync.Application.Interfaces.Repositories
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllAsync();
        Task<BranchDto?> GetBranchByIdAsync(int id);
        Task CreateBranchAsync(BranchDto branchDto);
        Task UpdateBranchAsync(BranchDto branchDto);
        Task DeleteBranchAsync(int id);
    }
}
