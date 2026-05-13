using Salesync.Application.Dtos.BranchDto;

namespace Salesync.Application.Interfaces.Services
{
    public interface IBranchService
    {
        Task<IEnumerable<BranchDto>> GetAllAsync();
        Task<BranchDto?> GetBranchByIdAsync(int id);
        Task<BranchDto> CreateBranchAsync(CreateBranchDto createBranchDto);
        Task<BranchDto> UpdateBranchAsync(int id, UpdateBranchDto updateBranchDto);
        Task DeleteAsync(int id);
    }
}
