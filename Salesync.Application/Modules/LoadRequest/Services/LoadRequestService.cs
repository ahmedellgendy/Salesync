using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.LoadRequest.Dtos;
using Salesync.Application.Modules.LoadRequest.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Modules.LoadRequest.Entities;

using LoadRequestEntity = Salesync.Domain.Modules.LoadRequest.Entities.LoadRequest;
using SalesRepEntity = Salesync.Domain.Modules.SalesRep.Entities.SalesRep;

namespace Salesync.Application.Modules.LoadRequest.Services
{
    public class LoadRequestService : ILoadRequestService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly IInventoryService _inventoryService;
        private readonly IValidator<CreateLoadRequestDto> _createValidator;
        private readonly IValidator<ApproveLoadRequestDto> _approveValidator;
        private readonly IValidator<ConfirmLoadRequestDto> _confirmValidator;
        private readonly IValidator<RejectLoadRequestDto> _rejectValidator;

        public LoadRequestService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser, IInventoryService inventoryService,
            IValidator<CreateLoadRequestDto> createValidator,
            IValidator<ApproveLoadRequestDto> approveValidator,
            IValidator<ConfirmLoadRequestDto> confirmValidator,
            IValidator<RejectLoadRequestDto> rejectValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _inventoryService = inventoryService;
            _createValidator = createValidator;
            _approveValidator = approveValidator;
            _confirmValidator = confirmValidator;
            _rejectValidator = rejectValidator;
        }

        public async Task<IEnumerable<LoadRequestDto>> GetAllAsync()
        {
            var requests = await _unitOfWork.LoadRequests
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LoadRequestDto>>(requests);
        }

        public async Task<IEnumerable<LoadRequestDto>> GetPendingAsync()
        {
            var requests = await _unitOfWork.LoadRequests
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                .Where(x => x.IsActive && x.Status == LoadRequestStatus.Pending)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LoadRequestDto>>(requests);
        }

        public async Task<IEnumerable<LoadRequestDto>> GetApprovedAsync()
        {
            var requests = await _unitOfWork.LoadRequests
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                .Where(x => x.IsActive && x.Status == LoadRequestStatus.Approved)
                .OrderByDescending(x => x.RequestDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<LoadRequestDto>>(requests);
        }

        public async Task<LoadRequestDto> GetByIdAsync(int id)
        {
            var request = await GetRequestWithDetailsAsync(id);
            return _mapper.Map<LoadRequestDto>(request);
        }

        public async Task<IEnumerable<LoadRequestDto>> GetBySalesRepAsync(int salesRepId)
        {
            var requests = await _unitOfWork.LoadRequests
                            .GetQueryable()
                            .Include(x => x.SalesRep)
                            .Include(x => x.Warehouse)
                            .Include(x => x.Items)
                            .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                            .OrderByDescending(x => x.RequestDate)
                            .ToListAsync();

            return _mapper.Map<IEnumerable<LoadRequestDto>>(requests);
        }

        public async Task<LoadRequestDto> CreateAsync(CreateLoadRequestDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("Current user is not authenticated.");

            var salesRep = await GetLoadRequestSalesRepAsync(dto.SalesRepId);

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId)
                ?? throw new KeyNotFoundException($"Warehouse with id {dto.WarehouseId} not found.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("Cannot create load request for inactive warehouse.");

            EnsureNoDuplicateProducts(dto.Items.Select(x => x.ProductId));

            var request = new LoadRequestEntity
            {
                LoadRequestNumber = GenerateLoadRequestNumber(),
                SalesRepId = salesRep.Id,
                WarehouseId = dto.WarehouseId,
                RequestDate = DateTime.UtcNow,
                Status = LoadRequestStatus.Pending,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            foreach (var itemDto in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId)
                    ?? throw new KeyNotFoundException($"Product with id {itemDto.ProductId} not found.");

                if (!product.IsActive)
                    throw new InvalidOperationException($"Product with id {itemDto.ProductId} is inactive.");

                request.Items.Add(new LoadRequestItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ItemCode = product.ItemCode,
                    RequestedQuantity = itemDto.RequestedQuantity,
                    ApprovedQuantity = 0,
                    ConfirmedQuantity = 0,
                    Notes = itemDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            }

            await _unitOfWork.LoadRequests.AddAsync(request);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(request.Id);
        }

        public async Task<LoadRequestDto> ApproveAsync(int id, ApproveLoadRequestDto dto)
        {
            var validationResult = await _approveValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var request = await GetRequestWithDetailsAsync(id);

            if (request.Status != LoadRequestStatus.Pending)
                throw new InvalidOperationException("Only pending load requests can be approved.");

            EnsureNoDuplicateItems(dto.Items.Select(x => x.LoadRequestItemId));
            EnsureAllRequestItemsProvided(
                request.Items.Select(x => x.Id),
                dto.Items.Select(x => x.LoadRequestItemId));

            foreach (var itemDto in dto.Items)
            {
                var item = request.Items.First(x => x.Id == itemDto.LoadRequestItemId);

                if (itemDto.ApprovedQuantity > item.RequestedQuantity)
                    throw new InvalidOperationException(
                        $"Approved quantity cannot exceed requested quantity for product {item.ProductName}.");

                item.ApprovedQuantity = itemDto.ApprovedQuantity;
                item.UpdatedAt = DateTime.UtcNow;
            }

            if (request.Items.Sum(x => x.ApprovedQuantity) <= 0)
                throw new InvalidOperationException("At least one item must have approved quantity greater than zero.");

            request.Status = LoadRequestStatus.Approved;
            request.ApprovedByUserId = _currentUser.UserId;
            request.ApprovedAt = DateTime.UtcNow;
            request.Notes = dto.Notes ?? request.Notes;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.LoadRequests.Update(request);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(request.Id);
        }

        public async Task<LoadRequestDto> RejectAsync(int id, RejectLoadRequestDto dto)
        {
            var validationResult = await _rejectValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var request = await GetRequestWithDetailsAsync(id);

            if (request.Status != LoadRequestStatus.Pending)
                throw new InvalidOperationException("Only pending load requests can be rejected.");

            request.Status = LoadRequestStatus.Rejected;
            request.RejectedByUserId = _currentUser.UserId;
            request.RejectedAt = DateTime.UtcNow;
            request.RejectionReason = dto.RejectionReason;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.LoadRequests.Update(request);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(request.Id);
        }

        public async Task<LoadRequestDto> ConfirmWarehouseAsync(int id, ConfirmLoadRequestDto dto)
        {
            var validationResult = await _confirmValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var request = await GetRequestWithDetailsAsync(id);

            if (request.Status != LoadRequestStatus.Approved)
                throw new InvalidOperationException("Only approved load requests can be warehouse confirmed.");

            EnsureNoDuplicateItems(dto.Items.Select(x => x.LoadRequestItemId));
            EnsureAllRequestItemsProvided(
                request.Items.Select(x => x.Id),
                dto.Items.Select(x => x.LoadRequestItemId));

            foreach (var itemDto in dto.Items)
            {
                var item = request.Items.First(x => x.Id == itemDto.LoadRequestItemId);

                if (itemDto.ConfirmedQuantity > item.ApprovedQuantity)
                    throw new InvalidOperationException(
                        $"Confirmed quantity cannot exceed approved quantity for product {item.ProductName}.");
            }

            if (dto.Items.Sum(x => x.ConfirmedQuantity) <= 0)
                throw new InvalidOperationException("At least one item must have confirmed quantity greater than zero.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var itemDto in dto.Items)
                {
                    var item = request.Items.First(x => x.Id == itemDto.LoadRequestItemId);

                    item.ConfirmedQuantity = itemDto.ConfirmedQuantity;
                    item.UpdatedAt = DateTime.UtcNow;

                    if (itemDto.ConfirmedQuantity > 0)
                    {
                        await _inventoryService.StockOutAsync(
                            item.ProductId,
                            request.WarehouseId,
                            itemDto.ConfirmedQuantity,
                            StockMovementSource.LoadRequest,
                            request.Id,
                            request.LoadRequestNumber,
                            $"Stock out for load request {request.LoadRequestNumber}");

                        await IncreaseSalesRepInventoryAsync(
                              request.SalesRepId,
                              item.ProductId,
                              itemDto.ConfirmedQuantity,
                              SalesRepInventoryMovementSource.LoadRequest,
                              request.Id,
                              request.LoadRequestNumber,
                              $"Stock in for load request {request.LoadRequestNumber}");
                    }
                }

                request.Status = LoadRequestStatus.WarehouseConfirmed;
                request.ConfirmedByUserId = _currentUser.UserId;
                request.ConfirmedAt = DateTime.UtcNow;
                request.Notes = dto.Notes ?? request.Notes;
                request.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.LoadRequests.Update(request);
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

        public async Task CancelAsync(int id)
        {
            var request = await GetRequestWithDetailsAsync(id);

            if (request.Status == LoadRequestStatus.WarehouseConfirmed)
                throw new InvalidOperationException("Cannot cancel warehouse confirmed load request.");

            if (request.Status == LoadRequestStatus.Rejected)
                throw new InvalidOperationException("Cannot cancel rejected load request.");

            if (request.Status == LoadRequestStatus.Cancelled)
                throw new InvalidOperationException("Load request is already cancelled.");

            request.Status = LoadRequestStatus.Cancelled;
            request.CancelledByUserId = _currentUser.UserId;
            request.CancelledAt = DateTime.UtcNow;
            request.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.LoadRequests.Update(request);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<SalesRepInventoryDto>> GetSalesRepInventoryAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new InvalidOperationException("SalesRepId is required.");

            await EnsureSalesRepInventoryAccessAsync(salesRepId);

            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(salesRepId)
                ?? throw new KeyNotFoundException($"SalesRep with id {salesRepId} not found.");

            if (!salesRep.IsActive)
                throw new InvalidOperationException("Cannot get inventory for inactive sales rep.");

            var inventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Product)
                .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                .OrderBy(x => x.Product.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepInventoryDto>>(inventory);
        }

        public async Task<IEnumerable<SalesRepInventoryMovementDto>> GetSalesRepInventoryMovementsAsync(int salesRepId, int? productId = null)
        {
            if (salesRepId <= 0)
                throw new InvalidOperationException("SalesRepId is required.");

            if (productId.HasValue && productId.Value <= 0)
                throw new InvalidOperationException("ProductId must be greater than zero.");

            await EnsureSalesRepInventoryAccessAsync(salesRepId);

            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(salesRepId)
                ?? throw new KeyNotFoundException($"SalesRep with id {salesRepId} not found.");

            if (!salesRep.IsActive)
                throw new InvalidOperationException("Cannot get inventory movements for inactive sales rep.");

            var query = _unitOfWork.SalesRepInventoryMovements
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Product)
                .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(x => x.ProductId == productId.Value);

            var movements = await query
                .OrderByDescending(x => x.MovementDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepInventoryMovementDto>>(movements);
        }

        #region Helper Methods

        private async Task<LoadRequestEntity> GetRequestWithDetailsAsync(int id)
        {
            var request = await _unitOfWork.LoadRequests
                .GetQueryable()
                .Include(x => x.SalesRep)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (request == null)
                throw new KeyNotFoundException($"Load request with id {id} not found.");

            return request;
        }

        private async Task<SalesRepEntity> GetLoadRequestSalesRepAsync(int? dtoSalesRepId)
        {
            var isSalesRepUser = string.Equals(_currentUser.Role, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (isSalesRepUser)
            {
                return (await _unitOfWork.SalesReps.FindAsync(x => x.UserId == _currentUser.UserId && x.IsActive)).FirstOrDefault()
                    ?? throw new UnauthorizedAccessException("SalesRep not found for current user.");
            }

            if (!dtoSalesRepId.HasValue)
                throw new InvalidOperationException("SalesRepId is required for admin or supervisor load request creation.");

            var salesRep = await _unitOfWork.SalesReps.GetByIdAsync(dtoSalesRepId.Value)
                ?? throw new KeyNotFoundException($"SalesRep with id {dtoSalesRepId.Value} not found.");

            if (!salesRep.IsActive)
                throw new InvalidOperationException("Cannot create load request for inactive sales rep.");

            return salesRep;
        }

        private static void EnsureNoDuplicateProducts(IEnumerable<int> productIds)
        {
            var duplicateProductId = productIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateProductId > 0)
                throw new InvalidOperationException($"Duplicate product id {duplicateProductId} is not allowed in the same load request.");
        }

        private static void EnsureNoDuplicateItems(IEnumerable<int> itemIds)
        {
            var duplicateItemId = itemIds
                .GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (duplicateItemId > 0)
                throw new InvalidOperationException($"Duplicate load request item id {duplicateItemId} is not allowed.");
        }

        private static void EnsureAllRequestItemsProvided(IEnumerable<int> requestItemIds, IEnumerable<int> dtoItemIds)
        {
            var missingIds = requestItemIds
                .Except(dtoItemIds)
                .ToList();

            if (missingIds.Any())
                throw new InvalidOperationException("All load request items must be provided.");
        }

        private static string GenerateLoadRequestNumber()
        {
            return $"LR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        }

        private async Task IncreaseSalesRepInventoryAsync(
          int salesRepId,
          int productId,
          int quantity,
          SalesRepInventoryMovementSource source,
          int? sourceId = null,
          string? sourceNumber = null,
          string? notes = null)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Quantity must be greater than zero.");

            var inventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.SalesRepId == salesRepId &&
                    x.ProductId == productId &&
                    x.IsActive);

            var isNewInventory = false;

            if (inventory == null)
            {
                inventory = new SalesRepInventory
                {
                    SalesRepId = salesRepId,
                    ProductId = productId,
                    Quantity = 0,
                    LastUpdatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                isNewInventory = true;

                await _unitOfWork.SalesRepInventories.AddAsync(inventory);
            }

            inventory.Quantity += quantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;
            inventory.UpdatedAt = DateTime.UtcNow;

            var movement = new SalesRepInventoryMovement
            {
                SalesRepId = salesRepId,
                ProductId = productId,
                Quantity = quantity,
                MovementType = SalesRepInventoryMovementType.In,
                Source = source,
                SourceId = sourceId,
                SourceNumber = sourceNumber,
                MovementDate = DateTime.UtcNow,
                Notes = notes,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            if (!isNewInventory)
                _unitOfWork.SalesRepInventories.Update(inventory);

            await _unitOfWork.SalesRepInventoryMovements.AddAsync(movement);
        }

        private async Task EnsureSalesRepInventoryAccessAsync(int salesRepId)
        {
            var isSalesRepUser = string.Equals(
                _currentUser.Role,
                "SalesRep",
                StringComparison.OrdinalIgnoreCase);

            if (!isSalesRepUser)
                return;

            var currentSalesRep = (await _unitOfWork.SalesReps
                .FindAsync(x => x.UserId == _currentUser.UserId && x.IsActive))
                .FirstOrDefault()
                ?? throw new UnauthorizedAccessException("SalesRep not found for current user.");

            if (currentSalesRep.Id != salesRepId)
                throw new UnauthorizedAccessException("You cannot access another sales rep inventory.");
        }

        #endregion


    }
}
