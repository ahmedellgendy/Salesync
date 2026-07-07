using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Inventory.Dtos;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Modules.Inventory.Entities;

namespace Salesync.Application.Modules.Inventory.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InventoryService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<StockBalanceDto> CreateOpeningBalanceAsync(CreateOpeningBalanceDto dto)
        {
            #region Validations

            if (dto.ProductId <= 0)
                throw new InvalidOperationException("ProductId is required.");

            if (dto.WarehouseId <= 0)
                throw new InvalidOperationException("WarehouseId is required.");

            if (dto.Quantity < 0)
                throw new InvalidOperationException("Quantity cannot be negative.");

            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId)
                ?? throw new KeyNotFoundException($"Product with id {dto.ProductId} not found.");

            if (!product.IsActive)
                throw new InvalidOperationException("Cannot create opening balance for inactive product.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId)
                ?? throw new KeyNotFoundException($"Warehouse with id {dto.WarehouseId} not found.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("Cannot create opening balance for inactive warehouse.");

            var existingBalance = await _unitOfWork.StockBalances
                .GetQueryable()
                .FirstOrDefaultAsync(s => s.ProductId == dto.ProductId && s.WarehouseId == dto.WarehouseId);

            if (existingBalance != null)
                throw new InvalidOperationException("Opening balance already exists for this product in this warehouse.");

            #endregion

            var stockBalance = new StockBalance
            {
                ProductId = dto.ProductId,
                WarehouseId = dto.WarehouseId,
                Quantity = dto.Quantity,
                LastUpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var movement = new StockMovement
            {
                ProductId = dto.ProductId,
                WarehouseId = dto.WarehouseId,
                Quantity = dto.Quantity,
                MovementType = StockMovementType.In,
                Source = StockMovementSource.OpeningBalance,
                MovementDate = DateTime.UtcNow,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.StockBalances.AddAsync(stockBalance);
            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await GetBalanceAsync(dto.ProductId, dto.WarehouseId);
        }

        public async Task<StockBalanceDto> GetBalanceAsync(int productId, int warehouseId)
        {
            var balances = await _unitOfWork.StockBalances
                .GetQueryable()
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId);

            if (balances == null)
                throw new KeyNotFoundException($"Stock balance for product id {productId} in warehouse id {warehouseId} not found.");

            return _mapper.Map<StockBalanceDto>(balances);
        }

        public async Task<IEnumerable<StockBalanceDto>> GetAllBalancesAsync()
        {
            var balances = await _unitOfWork.StockBalances
               .GetQueryable()
               .Include(x => x.Product)
               .Include(x => x.Warehouse)
               .Where(x => x.IsActive)
               .ToListAsync();

            return _mapper.Map<IEnumerable<StockBalanceDto>>(balances);
        }

        public async Task<IEnumerable<StockMovementDto>> GetMovementsAsync(int? productId = null, int? warehouseId = null)
        {
            var query = _unitOfWork.StockMovements
                .GetQueryable()
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .Where(x => x.IsActive)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            var movements = await query
                .OrderByDescending(x => x.MovementDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<StockMovementDto>>(movements);
        }

        public async Task<StockMovementDto> StockInAsync(int productId, int warehouseId, int quantity, StockMovementSource source = StockMovementSource.StockAdjustment, int? sourceId = null, string? sourceNumber = null, string? notes = null)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            var balance = await GetOrCreateBalanceAsync(productId, warehouseId);

            balance.Quantity += quantity;
            balance.LastUpdatedAt = DateTime.UtcNow;
            balance.UpdatedAt = DateTime.UtcNow;

            var movement = new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = quantity,
                MovementType = StockMovementType.In,
                Source = source,
                SourceId = sourceId,
                SourceNumber = sourceNumber,
                MovementDate = DateTime.UtcNow,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _unitOfWork.StockBalances.Update(balance);
            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await GetMovementDtoAsync(movement.Id);
        }

        public async Task<StockMovementDto> StockOutAsync(int productId, int warehouseId, int quantity, StockMovementSource source = StockMovementSource.StockAdjustment, int? sourceId = null, string? sourceNumber = null, string? notes = null)
        {
            #region Validations

            if (quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            var product = await _unitOfWork.Products.GetByIdAsync(productId)
                ?? throw new KeyNotFoundException($"Product with id {productId} not found.");

            if (!product.IsActive)
                throw new InvalidOperationException("Cannot update stock for inactive product.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId)
                ?? throw new KeyNotFoundException($"Warehouse with id {warehouseId} not found.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("Cannot update stock for inactive warehouse.");

            #endregion

            var balance = await _unitOfWork.StockBalances
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.WarehouseId == warehouseId);

            if (balance == null)
                throw new InvalidOperationException("Stock balance not found.");

            if (balance.Quantity < quantity)
                throw new InvalidOperationException("Product Unavailable in Requested Quantity.");

            balance.Quantity -= quantity;
            balance.LastUpdatedAt = DateTime.UtcNow;
            balance.UpdatedAt = DateTime.UtcNow;

            var movement = new StockMovement
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = quantity,
                MovementType = StockMovementType.Out,
                Source = source,
                SourceId = sourceId,
                SourceNumber = sourceNumber,
                MovementDate = DateTime.UtcNow,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _unitOfWork.StockBalances.Update(balance);
            await _unitOfWork.StockMovements.AddAsync(movement);
            await _unitOfWork.CompleteAsync();

            return await GetMovementDtoAsync(movement.Id);
        }


        #region Helper Method

        private async Task<StockBalance> GetOrCreateBalanceAsync(int productId, int warehouseId)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(productId)
                ?? throw new KeyNotFoundException($"Product with id {productId} not found.");

            if (!product.IsActive)
                throw new InvalidOperationException("Cannot update stock for inactive product.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(warehouseId)
                ?? throw new KeyNotFoundException($"Warehouse with id {warehouseId} not found.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("Cannot update stock for inactive warehouse.");

            var balance = await _unitOfWork.StockBalances
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.WarehouseId == warehouseId);

            if (balance != null)
                return balance;

            balance = new StockBalance
            {
                ProductId = productId,
                WarehouseId = warehouseId,
                Quantity = 0,
                LastUpdatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.StockBalances.AddAsync(balance);

            return balance;
        }

        private async Task<StockMovementDto> GetMovementDtoAsync(int movementId)
        {
            var movement = await _unitOfWork.StockMovements
                .GetQueryable()
                .Include(x => x.Product)
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.Id == movementId);

            if (movement == null)
                throw new KeyNotFoundException($"Stock movement with id {movementId} not found.");

            return _mapper.Map<StockMovementDto>(movement);
        }


        #endregion
    }
}