using AutoMapper;
using FluentValidation;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class SalesRepService : ISalesRepService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSalesRepDto> _createSalesRepValidator;
        private readonly IValidator<UpdateSalesRepDto> _updateSalesRepValidator;
        public SalesRepService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateSalesRepDto> createSalesRepValidator, IValidator<UpdateSalesRepDto> updateSalesRepValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createSalesRepValidator = createSalesRepValidator;
            _updateSalesRepValidator = updateSalesRepValidator;
        }
        public async Task<IEnumerable<SalesRepDto>> GetAllAsync()
        {
            var salesReps = await _unitOfWork.SalesReps.GetAllAsync();
            return _mapper.Map<IEnumerable<SalesRepDto>>(salesReps);
        }
        public async Task<SalesRepDto> GetByIdAsync(int id)
        {
            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SalesRep with id {id} not found.");
            return _mapper.Map<SalesRepDto>(salesRep);
        }
        public async Task<SalesRepDto> CreateAsync(CreateSalesRepDto dto)
        {
            var validationResult = await _createSalesRepValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // check if the sales rep code is unique
            var salesRepCodeExists = await _unitOfWork.SalesReps.ExistsAsync(x => x.SalesRepCode == dto.SalesRepCode);
            if (salesRepCodeExists)
                throw new ArgumentException($"SalesRep with code {dto.SalesRepCode} already exists.");

            // Check if the branch exists
            var branchExists = await _unitOfWork.Branches.ExistsAsync(x => x.Id == dto.BranchId);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch with id {dto.BranchId} not found.");

            var salesRep = _mapper.Map<Domain.Modules.SalesRep.Entities.SalesRep>(dto);
            await _unitOfWork.SalesReps.AddAsync(salesRep);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<SalesRepDto>(salesRep);
        }
        public async Task<SalesRepDto> UpdateAsync(int id, UpdateSalesRepDto dto)
        {
            var validationResult = await _updateSalesRepValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SalesRep with id {id} not found.");
            _mapper.Map(dto, salesRep);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<SalesRepDto>(salesRep);
        }
        public async Task DeleteAsync(int id)
        {
            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"SalesRep with id {id} not found.");
            salesRep.IsActive = false;
            await _unitOfWork.CompleteAsync();
        }

    }
}
