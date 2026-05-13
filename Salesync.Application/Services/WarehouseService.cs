using AutoMapper;
using FluentValidation;
using Salesync.Application.Dtos.WarehouseDto;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Domain.Entities;

namespace Salesync.Application.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateWarehouseDto> _createValidator;
        private readonly IValidator<UpdateWarehouseDto> _updateValidator;

        public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateWarehouseDto> createValidator, IValidator<UpdateWarehouseDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }


        public async Task<IEnumerable<WarehouseDto>> GetAllAsync()
        {
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
        }
        public async Task<WarehouseDto?> GetByIdAsync(int id)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            return warehouse == null ? null : _mapper.Map<WarehouseDto>(warehouse);
        }
        public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto warehouseDto)
        {
            // Validate the incoming DTO
            var validationResult = await _createValidator.ValidateAsync(warehouseDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // Check if branch exists
            var branch = await _unitOfWork.Branches
                .GetByIdAsync(warehouseDto.BranchId);

            if (branch == null)
                throw new Exception("Branch does not exist.");


            var warehouse = _mapper.Map<Warehouse>(warehouseDto);

            // Create the warehouse in the database
            await _unitOfWork.Warehouses.CreateAsync(warehouse);

            // Save changes to the database
            await _unitOfWork.CompleteAsync();

            // Return the created warehouse as a DTO
            return _mapper.Map<WarehouseDto>(warehouse);

        }
        public async Task<WarehouseDto> UpdateAsync(int id, UpdateWarehouseDto warehouseDto)
        {
            // Validate the incoming DTO
            var validationResult = await _updateValidator.ValidateAsync(warehouseDto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // get the existing warehouse
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
                throw new KeyNotFoundException($"Warehouse with ID {id} not found");

            if (warehouseDto.BranchId.HasValue)
            {
                var branch = await _unitOfWork.Branches
                    .GetByIdAsync(warehouseDto.BranchId.Value);

                if (branch == null)
                    throw new KeyNotFoundException("Branch does not exist.");
            }

            _mapper.Map(warehouseDto, warehouse);

            // Update the warehouse in the database
            _unitOfWork.Warehouses.UpdateAsync(warehouse);

            // Save changes to the database
            await _unitOfWork.CompleteAsync();

            // Return the updated warehouse as a DTO
            return _mapper.Map<WarehouseDto>(warehouse);

        }
        public async Task<bool> DeleteAsync(int id)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
                throw new KeyNotFoundException($"Warehouse with ID {id} not found");

            _unitOfWork.Warehouses.Delete(warehouse);
            await _unitOfWork.CompleteAsync();
            return true;

        }

    }
}
