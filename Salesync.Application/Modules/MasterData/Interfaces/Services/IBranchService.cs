using Salesync.Application.Modules.MasterData.Dtos.BranchDto;

namespace Salesync.Application.Modules.MasterData.Interfaces.Services
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
