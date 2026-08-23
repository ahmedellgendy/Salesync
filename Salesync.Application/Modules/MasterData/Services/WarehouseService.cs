using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.Dtos.WarehouseDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Application.Modules.MasterData.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateWarehouseDto> _createValidator;
        private readonly IValidator<UpdateWarehouseDto> _updateValidator;

        public WarehouseService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateWarehouseDto> createValidator,
            IValidator<UpdateWarehouseDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<WarehouseDto>> GetAllAsync()
        {
            var warehouses = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Branch)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
        }

        public async Task<WarehouseDto?> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid warehouse id.");

            var warehouse = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            return warehouse == null
                ? null
                : _mapper.Map<WarehouseDto>(warehouse);
        }

        public async Task<WarehouseDto> CreateAsync(
            CreateWarehouseDto warehouseDto)
        {
            var validationResult =
                await _createValidator.ValidateAsync(warehouseDto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            warehouseDto.WarehouseCode = warehouseDto.WarehouseCode.Trim();
            warehouseDto.Name = warehouseDto.Name.Trim();

            var branch = await _unitOfWork.Branches
                .GetByIdAsync(warehouseDto.BranchId);

            if (branch == null)
            {
                throw new KeyNotFoundException(
                    $"Branch with ID {warehouseDto.BranchId} does not exist.");
            }

            var warehouseCodeExists = await _unitOfWork.Warehouses
                .GetQueryable()
                .AnyAsync(x =>
                    x.WarehouseCode == warehouseDto.WarehouseCode);

            if (warehouseCodeExists)
            {
                throw new InvalidOperationException(
                    $"Warehouse code '{warehouseDto.WarehouseCode}' already exists.");
            }

            var warehouse = _mapper.Map<Warehouse>(warehouseDto);

            warehouse.Branch = branch;

            await _unitOfWork.Warehouses.AddAsync(warehouse);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<WarehouseDto>(warehouse);
        }

        public async Task<WarehouseDto> UpdateAsync(
            int id,
            UpdateWarehouseDto warehouseDto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid warehouse id.");

            var validationResult =
                await _updateValidator.ValidateAsync(warehouseDto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            if (!string.IsNullOrWhiteSpace(warehouseDto.Name))
            {
                warehouseDto.Name = warehouseDto.Name.Trim();
            }

            var warehouse = await _unitOfWork.Warehouses
                .GetQueryable()
                .Include(x => x.Branch)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (warehouse == null)
            {
                throw new KeyNotFoundException(
                    $"Warehouse with ID {id} not found.");
            }

            Branch? branch = null;

            if (warehouseDto.BranchId.HasValue)
            {
                branch = await _unitOfWork.Branches
                    .GetByIdAsync(warehouseDto.BranchId.Value);

                if (branch == null)
                {
                    throw new KeyNotFoundException(
                        $"Branch with ID {warehouseDto.BranchId.Value} does not exist.");
                }
            }

            _mapper.Map(warehouseDto, warehouse);

            if (branch != null)
            {
                warehouse.Branch = branch;
            }

            warehouse.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<WarehouseDto>(warehouse);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid warehouse id.");

            var warehouse = await _unitOfWork.Warehouses
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (warehouse == null)
            {
                throw new KeyNotFoundException(
                    $"Warehouse with ID {id} not found.");
            }

            warehouse.IsActive = false;
            warehouse.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Warehouses.Update(warehouse);
            await _unitOfWork.CompleteAsync();

            return true;
        }
    }
}