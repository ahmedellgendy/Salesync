using AutoMapper;
using Salesync.Application.Dtos.BranchDto;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;

namespace Salesync.Application.Services
{
    public class BranchService : IBranchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BranchService(IUnitOfWork unitOfWork,IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<IEnumerable<BranchDto>> GetAllAsync()
        { 
            var branches = await _unitOfWork.Branches.GetAllAsync();
            return _mapper.Map<IEnumerable<BranchDto>>(branches);
        }
        public async Task<BranchDto?> GetBranchByIdAsync(int id)
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            return _mapper.Map<BranchDto?>(branch);
        }
        public Task CreateBranchAsync(BranchDto branchDto)
        {
            throw new NotImplementedException();
        }
        public Task UpdateBranchAsync(BranchDto branchDto)
        {
            throw new NotImplementedException();
        }
        public Task DeleteBranchAsync(int id)
        {
            throw new NotImplementedException();
        }


    }
}
