using AutoMapper;
using FluentValidation;
using Salesync.Application.Dtos.BranchDto;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Domain.Entities;

namespace Salesync.Application.Services
{
    public class BranchService : IBranchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateBranchDto> _createValidator;
        private readonly IValidator<UpdateBranchDto> _updateValidator;

        public BranchService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateBranchDto> createValidator, IValidator<UpdateBranchDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }
        public async Task<IEnumerable<BranchDto>> GetAllAsync()
        {
            var branches = await _unitOfWork.Branches.GetAllAsync();
            return _mapper.Map<IEnumerable<BranchDto>>(branches);
        }
        public async Task<BranchDto?> GetBranchByIdAsync(int id)
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null || !branch.IsActive)
                return null;
            return _mapper.Map<BranchDto?>(branch);
        }
        public async Task<BranchDto> CreateBranchAsync(CreateBranchDto createBranchDto)
        {
            var validationResult = await _createValidator.ValidateAsync(createBranchDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var branch = _mapper.Map<Branch>(createBranchDto);
            branch.CreatedAt = DateTime.UtcNow;
            branch.IsActive = true;

            await _unitOfWork.Branches.AddAsync(branch);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<BranchDto>(branch);
        }
        public async Task<BranchDto> UpdateBranchAsync(int id, UpdateBranchDto updateBranchDto)
        {
            var validationResult = await _updateValidator.ValidateAsync(updateBranchDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var updatedBranch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (updatedBranch == null || !updatedBranch.IsActive)
                throw new KeyNotFoundException($"Branch with Id {id} not found or inactive.");

            _mapper.Map(updateBranchDto, updatedBranch);
            updatedBranch.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<BranchDto>(updatedBranch);
        }
        public async Task DeleteAsync(int id)
        {
            var branch = await _unitOfWork.Branches.GetByIdAsync(id);
            if (branch == null || !branch.IsActive)
                throw new KeyNotFoundException($"Branch with Id {id} not found or already inactive.");

            branch.IsActive = false;
            branch.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Branches.UpdateAsync(branch);
            await _unitOfWork.CompleteAsync();
        }
    }
}
