using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.UnloadRequest.Dtos;
using Salesync.Application.Modules.UnloadRequest.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.UnloadRequest;
using Salesync.Domain.Modules.Inventory.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.UnloadRequest.Entities;

namespace Salesync.Application.Modules.UnloadRequest.Services
{
    public class SalesRepUnloadRequestService : ISalesRepUnloadRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public SalesRepUnloadRequestService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<SalesRepUnloadRequestDto> CreateAsync(CreateSalesRepUnloadRequestDto dto)
        {
            if (dto.SalesRepSessionId <= 0)
                throw new ArgumentException("Invalid sales rep session id.");

            if (dto.WarehouseId <= 0)
                throw new ArgumentException("Invalid warehouse id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.SalesRepSessionId &&
                    x.SalesRepId == salesRep.Id &&
                    x.IsActive);

            if (session is null)
                throw new KeyNotFoundException("Session not found for current sales rep.");

            if (session.Status == DayStatus.Closed || session.EndTime.HasValue)
                throw new InvalidOperationException("Cannot create unload request for a closed session.");

            if (session.IsStockSettled)
                throw new InvalidOperationException("Stock is already settled for this session.");

            var warehouse = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.WarehouseId &&
                    x.IsActive);

            if (warehouse is null)
                throw new KeyNotFoundException("Warehouse not found.");

            var hasPendingRequest = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.SalesRepSessionId == session.Id &&
                    x.IsActive &&
                    (x.Status == UnloadRequestStatus.Draft ||
                     x.Status == UnloadRequestStatus.PendingWarehouse));

            if (hasPendingRequest)
                throw new InvalidOperationException("There is already an active unload request for this session.");

            var inventories = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Product)
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.IsActive &&
                    x.Quantity > 0)
                .OrderBy(x => x.Product.Name)
                .ToListAsync();

            if (inventories.Count == 0)
                throw new InvalidOperationException("No remaining stock found for current sales rep.");

            var request = new SalesRepUnloadRequest
            {
                RequestNumber = GenerateRequestNumber(),
                SalesRepId = salesRep.Id,
                SalesRepSessionId = session.Id,
                WarehouseId = warehouse.Id,
                BranchId = salesRep.BranchId,
                Status = UnloadRequestStatus.PendingWarehouse,
                RequestedAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow,
                RequestedByUserId = _currentUser.UserId,
                SalesRepNotes = dto.SalesRepNotes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            foreach (var inventory in inventories)
            {
                var product = inventory.Product;

                if (product is null)
                    continue;

                var unitsPerLargeUnit = product.UnitsPerLargeUnit <= 0
                    ? 1
                    : product.UnitsPerLargeUnit;

                var requestedLargeQuantity = inventory.Quantity / unitsPerLargeUnit;
                var requestedSmallQuantity = inventory.Quantity % unitsPerLargeUnit;

                request.Items.Add(new SalesRepUnloadRequestItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ItemCode = product.ItemCode,

                    SmallUnit = string.IsNullOrWhiteSpace(product.SmallUnit)
                        ? "قطعة"
                        : product.SmallUnit,

                    LargeUnit = string.IsNullOrWhiteSpace(product.LargeUnit)
                        ? "كرتونة"
                        : product.LargeUnit,

                    UnitsPerLargeUnit = unitsPerLargeUnit,

                    RequestedQuantity = inventory.Quantity,
                    RequestedLargeQuantity = requestedLargeQuantity,
                    RequestedSmallQuantity = requestedSmallQuantity,


                    ConfirmedQuantity = 0,
                    ConfirmedLargeQuantity = 0,
                    ConfirmedSmallQuantity = 0,
                    VarianceQuantity = 0,

                    SalesRepInventoryBeforeUnload = inventory.Quantity,
                    SalesRepInventoryAfterUnload = inventory.Quantity,

                    SalesRepNotes = dto.SalesRepNotes,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            if (!request.Items.Any())
                throw new InvalidOperationException("No valid items found for unload request.");

            RecalculateTotals(request);

            await _unitOfWork.SalesRepUnloadRequests.AddAsync(request);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(request.Id);
        }

        public async Task<IEnumerable<SalesRepUnloadRequestDto>> GetMyRequestsAsync()
        {
            var salesRep = await GetCurrentSalesRepAsync();

            var requests = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x =>
                    x.SalesRepId == salesRep.Id &&
                    x.IsActive)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var dtos = _mapper.Map<List<SalesRepUnloadRequestDto>>(requests);

            foreach (var dto in dtos)
            {
                await FillDisplayDataAsync(dto);
            }

            return dtos;
        }

        public async Task<SalesRepUnloadRequestDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid unload request id.");

            var request = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (request is null)
                throw new KeyNotFoundException($"Unload request with id {id} not found.");

            var dto = _mapper.Map<SalesRepUnloadRequestDto>(request);

            await FillDisplayDataAsync(dto);

            return dto;
        }

        public async Task<IEnumerable<SalesRepUnloadRequestDto>> GetPendingWarehouseRequestsAsync()
        {
            var requests = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x =>
                    x.IsActive &&
                    x.Status == UnloadRequestStatus.PendingWarehouse)
                .OrderBy(x => x.RequestedAt)
                .ToListAsync();

            var dtos = _mapper.Map<List<SalesRepUnloadRequestDto>>(requests);

            foreach (var dto in dtos)
            {
                await FillDisplayDataAsync(dto);
            }

            return dtos;
        }

        public async Task<SalesRepUnloadRequestDto> ConfirmWarehouseAsync(int id,ConfirmSalesRepUnloadRequestDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid unload request id.");

            if (dto.Items is null || dto.Items.Count == 0)
                throw new ArgumentException("Confirmation must contain at least one item.");

            var request = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (request is null)
                throw new KeyNotFoundException($"Unload request with id {id} not found.");

            if (request.Status != UnloadRequestStatus.PendingWarehouse)
                throw new InvalidOperationException("Only pending warehouse unload requests can be confirmed.");

            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.SalesRepSessionId &&
                    x.SalesRepId == request.SalesRepId &&
                    x.IsActive);

            if (session is null)
                throw new KeyNotFoundException("Sales rep session not found.");

            if (session.Status == DayStatus.Closed || session.EndTime.HasValue)
                throw new InvalidOperationException("Cannot confirm unload request for a closed session.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in request.Items)
                {
                    var confirmItem = dto.Items
                        .FirstOrDefault(x => x.UnloadRequestItemId == item.Id);

                    if (confirmItem is null)
                        throw new InvalidOperationException($"Missing confirmation quantity for item {item.ProductName}.");

                    if (confirmItem.ConfirmedLargeQuantity < 0)
                        throw new InvalidOperationException($"Confirmed quantity cannot be negative for item {item.ProductName}.");

                    if (confirmItem.ConfirmedSmallQuantity < 0)
                        throw new InvalidOperationException($"Confirmed small quantity cannot be negative for item {item.ProductName}.");

                    if (confirmItem.ConfirmedSmallQuantity >= item.UnitsPerLargeUnit)
                    {
                        throw new InvalidOperationException(
                            $"Confirmed small quantity for product {item.ProductName} must be less than {item.UnitsPerLargeUnit} {item.SmallUnit}.");
                    }

                    var confirmedQuantity =
                        (confirmItem.ConfirmedLargeQuantity * item.UnitsPerLargeUnit)
                        + confirmItem.ConfirmedSmallQuantity;

                    if (confirmedQuantity > item.RequestedQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Confirmed quantity for product {item.ProductName} cannot be greater than requested quantity.");
                    }

                    var salesRepInventory = await _unitOfWork.SalesRepInventories
                        .GetQueryable()
                        .FirstOrDefaultAsync(x =>
                            x.SalesRepId == request.SalesRepId &&
                            x.ProductId == item.ProductId &&
                            x.IsActive);

                    if (salesRepInventory is null)
                    {
                        throw new InvalidOperationException(
                            $"Sales rep inventory not found for product {item.ProductName}.");
                    }

                    if (salesRepInventory.Quantity < confirmedQuantity)
                    {
                        throw new InvalidOperationException(
                            $"Sales rep inventory is less than confirmed quantity for product {item.ProductName}.");
                    }

                    item.ConfirmedLargeQuantity = confirmItem.ConfirmedLargeQuantity;
                    item.ConfirmedSmallQuantity = confirmItem.ConfirmedSmallQuantity;
                    item.ConfirmedQuantity = confirmedQuantity;
                    item.VarianceQuantity = item.RequestedQuantity - item.ConfirmedQuantity;
                    item.WarehouseNotes = confirmItem.WarehouseNotes;
                    item.SalesRepInventoryBeforeUnload = salesRepInventory.Quantity;

                    if (confirmedQuantity > 0)
                    {
                        // 1. خصم من مخزون المندوب
                        salesRepInventory.Quantity -= confirmedQuantity;
                        salesRepInventory.LastUpdatedAt = DateTime.UtcNow;
                        salesRepInventory.UpdatedAt = DateTime.UtcNow;

                        item.SalesRepInventoryAfterUnload = salesRepInventory.Quantity;

                        var salesRepMovement = new SalesRepInventoryMovement
                        {
                            SalesRepId = request.SalesRepId,
                            ProductId = item.ProductId,
                            Quantity = confirmedQuantity,
                            MovementType = SalesRepInventoryMovementType.Out,
                            Source = SalesRepInventoryMovementSource.UnloadRequest,
                            SourceId = request.Id,
                            SourceNumber = request.RequestNumber,
                            MovementDate = DateTime.UtcNow,
                            Notes = $"Unload request confirmed by warehouse {request.RequestNumber}",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        _unitOfWork.SalesRepInventories.Update(salesRepInventory);
                        await _unitOfWork.SalesRepInventoryMovements.AddAsync(salesRepMovement);

                        // 2. إضافة لمخزون مخزن الشركة
                        var stockBalance = await _unitOfWork.StockBalances
                            .GetQueryable()
                            .FirstOrDefaultAsync(x =>
                                x.WarehouseId == request.WarehouseId &&
                                x.ProductId == item.ProductId &&
                                x.IsActive);

                        if (stockBalance is null)
                        {
                            stockBalance = new StockBalance
                            {
                                WarehouseId = request.WarehouseId,
                                ProductId = item.ProductId,
                                Quantity = confirmedQuantity,
                                LastUpdatedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true
                            };

                            await _unitOfWork.StockBalances.AddAsync(stockBalance);
                        }
                        else
                        {
                            stockBalance.Quantity += confirmedQuantity;
                            stockBalance.LastUpdatedAt = DateTime.UtcNow;
                            stockBalance.UpdatedAt = DateTime.UtcNow;

                            _unitOfWork.StockBalances.Update(stockBalance);
                        }

                        // 3. تسجيل حركة دخول لمخزن الشركة
                        var stockMovement = new StockMovement
                        {
                            WarehouseId = request.WarehouseId,
                            ProductId = item.ProductId,
                            Quantity = confirmedQuantity,
                            MovementType = StockMovementType.In,
                            Source = StockMovementSource.UnloadRequest,
                            SourceId = request.Id,
                            SourceNumber = request.RequestNumber,
                            MovementDate = DateTime.UtcNow,
                            Notes = $"Unload from sales rep to warehouse. Request: {request.RequestNumber}",
                            CreatedAt = DateTime.UtcNow,
                            IsActive = true
                        };

                        await _unitOfWork.StockMovements.AddAsync(stockMovement);
                    }
                    else
                    {
                        item.SalesRepInventoryAfterUnload = salesRepInventory.Quantity;
                    }

                    item.UpdatedAt = DateTime.UtcNow;
                }

                RecalculateTotals(request);

                request.Status = request.TotalVarianceQuantity == 0
                    ? UnloadRequestStatus.Confirmed
                    : UnloadRequestStatus.PartiallyConfirmed;

                request.WarehouseNotes = dto.WarehouseNotes;
                request.ConfirmedAt = DateTime.UtcNow;
                request.ConfirmedByUserId = _currentUser.UserId;
                request.UpdatedAt = DateTime.UtcNow;

                var isFullyConfirmed =
                     request.TotalVarianceQuantity == 0;

                session.IsStockSettled =
                    isFullyConfirmed;

                session.StockSettledAt =
                    isFullyConfirmed
                        ? DateTime.UtcNow
                        : null;

                
                session.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.SalesRepUnloadRequests.Update(request);
                _unitOfWork.SalesRepSessions.Update(session);

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return await GetByIdAsync(request.Id);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task CancelAsync(int id, string? reason)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid unload request id.");

            var request = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (request is null)
                throw new KeyNotFoundException($"Unload request with id {id} not found.");

            if (request.Status == UnloadRequestStatus.Confirmed ||
                request.Status == UnloadRequestStatus.PartiallyConfirmed)
            {
                throw new InvalidOperationException("Confirmed unload request cannot be cancelled.");
            }

            request.Status = UnloadRequestStatus.Cancelled;
            request.CancelledAt = DateTime.UtcNow;
            request.CancelledByUserId = _currentUser.UserId;
            request.CancellationReason = reason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepUnloadRequests.Update(request);
            await _unitOfWork.CompleteAsync();
        }

        private async Task<Salesync.Domain.Modules.SalesRep.Entities.SalesRep> GetCurrentSalesRepAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive);

            if (salesRep is null)
                throw new UnauthorizedAccessException("Sales rep not found for current user.");

            return salesRep;
        }
        public async Task<IEnumerable<SalesRepUnloadRequestDto>> GetAllAsync()
        {
            var requests =
                await _unitOfWork.SalesRepUnloadRequests
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.RequestedAt)
                    .ToListAsync();

            var dtos =
                _mapper.Map<List<SalesRepUnloadRequestDto>>(
                    requests);

            foreach (var dto in dtos)
            {
                await FillDisplayDataAsync(dto);
            }

            return dtos;
        }
        public async Task<SalesRepUnloadRequestDto> GetMyRequestByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid unload request id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var request = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.SalesRepId == salesRep.Id &&
                    x.IsActive);

            if (request is null)
                throw new KeyNotFoundException($"Unload request with id {id} not found for current sales rep.");

            var dto = _mapper.Map<SalesRepUnloadRequestDto>(request);

            await FillDisplayDataAsync(dto);

            return dto;
        }

        public async Task CancelMyRequestAsync(int id, string? reason)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid unload request id.");

            var salesRep = await GetCurrentSalesRepAsync();

            var request = await _unitOfWork.SalesRepUnloadRequests
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.SalesRepId == salesRep.Id &&
                    x.IsActive);

            if (request is null)
                throw new KeyNotFoundException($"Unload request with id {id} not found for current sales rep.");

            if (request.Status == UnloadRequestStatus.Confirmed ||
                request.Status == UnloadRequestStatus.PartiallyConfirmed)
            {
                throw new InvalidOperationException("Confirmed unload request cannot be cancelled.");
            }

            request.Status = UnloadRequestStatus.Cancelled;
            request.CancelledAt = DateTime.UtcNow;
            request.CancelledByUserId = _currentUser.UserId;
            request.CancellationReason = reason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepUnloadRequests.Update(request);
            await _unitOfWork.CompleteAsync();
        }

        private async Task FillDisplayDataAsync(SalesRepUnloadRequestDto dto)
        {
            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == dto.SalesRepId)
                .Select(x => new
                {
                    x.SalesRepCode,
                    x.Name
                })
                .FirstOrDefaultAsync();

            if (salesRep is not null)
            {
                dto.SalesRepCode = salesRep.SalesRepCode;
                dto.SalesRepName = salesRep.Name;
            }

            var warehouseName = await _unitOfWork.Warehouses
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.Id == dto.WarehouseId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync();

            dto.WarehouseName = warehouseName;
        }

        private static void RecalculateTotals(SalesRepUnloadRequest request)
        {
            request.TotalItems = request.Items.Count(x => x.IsActive);
            request.TotalRequestedQuantity = request.Items
                .Where(x => x.IsActive)
                .Sum(x => x.RequestedQuantity);

            request.TotalConfirmedQuantity = request.Items
                .Where(x => x.IsActive)
                .Sum(x => x.ConfirmedQuantity);

            request.TotalVarianceQuantity = request.Items
                .Where(x => x.IsActive)
                .Sum(x => x.VarianceQuantity);
        }

        private static string GenerateRequestNumber()
        {
            return $"UNL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        }
    }
}