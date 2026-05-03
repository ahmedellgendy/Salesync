using AutoMapper;
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

        public WarehouseService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }


        public async Task<IEnumerable<WarehouseDto>> GetAllAsync()
        {
            var warehouses = await _unitOfWork.Warehouses.GetAllAsync();
            return _mapper.Map<IEnumerable<WarehouseDto>>(warehouses);
        }
        public async Task<WarehouseDto?> GetByIdAsync(int id)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            return _mapper.Map<WarehouseDto>(warehouse);
        }
        public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto warehouseDto)
        {
            var warehouse = _mapper.Map<Warehouse>(warehouseDto);
            await _unitOfWork.Warehouses.CreateAsync(warehouse);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<WarehouseDto>(warehouse);

        }
        public async Task<WarehouseDto> UpdateAsync(int id, UpdateWarehouseDto warehouseDto)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                throw new Exception($"Warehouse {id} not found");
            }

            _mapper.Map(warehouseDto, warehouse);
            _unitOfWork.Warehouses.UpdateAsync(warehouse);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<WarehouseDto>(warehouse);

        }
        public async Task<bool> DeleteAsync(int id)
        {
            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(id);
            if (warehouse == null)
            {
                throw new Exception($"Warehouse {id} not found");
            }

            _unitOfWork.Warehouses.DeleteAsync(warehouse);
            await _unitOfWork.CompleteAsync();

            return true;
        }




    }
}
