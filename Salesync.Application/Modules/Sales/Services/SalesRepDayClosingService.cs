using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using ClosingEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosing;
using ClosingItemEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosingItem;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepDayClosingService : ISalesRepDayClosingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSalesRepDayClosingDto> _createValidator;
        private readonly IValidator<RejectSalesRepDayClosingDto> _rejectValidator;
        private readonly ICurrentUserService _currentUser;
        private readonly ISalesRepDayClosingCalculator _calculator;
        private readonly ISalesRepDayClosingSettlementService _settlementService;

        public SalesRepDayClosingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateSalesRepDayClosingDto> createValidator,
            IValidator<RejectSalesRepDayClosingDto> rejectValidator,
            ICurrentUserService currentUser,
            ISalesRepDayClosingCalculator calculator,
            ISalesRepDayClosingSettlementService settlementService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _rejectValidator = rejectValidator;
            _currentUser = currentUser;
            _calculator = calculator;
            _settlementService = settlementService;
        }

        public async Task<IEnumerable<SalesRepDayClosingDto>> GetAllAsync(SalesRepDayClosingFilterDto filter)
        {
            filter ??= new SalesRepDayClosingFilterDto();

            var query = GetClosingQuery();

            if (filter.SalesRepId.HasValue)
                query = query.Where(x => x.SalesRepId == filter.SalesRepId.Value);

            if (filter.SalesRepSessionId.HasValue)
                query = query.Where(x => x.SalesRepSessionId == filter.SalesRepSessionId.Value);

            if (filter.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == filter.WarehouseId.Value);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(x => x.ClosingDate >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                query = query.Where(x => x.ClosingDate <= filter.ToDate.Value);

            query = await ApplySalesRepAccessFilterAsync(query);

            var closings = await query
                .OrderByDescending(x => x.ClosingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepDayClosingDto>>(closings);
        }

        public async Task<SalesRepDayClosingDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid closing id.");

            var closing = await GetClosingQuery()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (closing == null)
                throw new KeyNotFoundException("Sales rep day closing not found.");

            await EnsureSalesRepCanAccessClosingAsync(closing.SalesRepId);

            return _mapper.Map<SalesRepDayClosingDto>(closing);
        }

        public async Task<IEnumerable<SalesRepDayClosingDto>> GetBySalesRepAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            await EnsureSalesRepCanAccessClosingAsync(salesRepId);

            var closings = await GetClosingQuery()
                .Where(x => x.SalesRepId == salesRepId)
                .OrderByDescending(x => x.ClosingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepDayClosingDto>>(closings);
        }

        public async Task<SalesRepDayClosingDto> CreateAsync(CreateSalesRepDayClosingDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var session = await GetSessionForClosingAsync(dto.SalesRepSessionId);

            await EnsureSalesRepCanAccessClosingAsync(session.SalesRepId);

            if (session.Status != DayStatus.Closed)
                throw new InvalidOperationException("Sales rep session must be closed before submitting day closing.");

            await EnsureWarehouseExistsAsync(dto.WarehouseId);

            await EnsureNoClosingExistsForSessionAsync(dto.SalesRepSessionId);

            await EnsureProductsExistAsync(dto.Items.Select(x => x.ProductId).ToList());

            await EnsureAllCurrentInventoryProductsProvidedAsync(session.SalesRepId, dto);

            await EnsureActualReturnedDoesNotExceedCurrentInventoryAsync(session.SalesRepId, dto);

            var calculation = await _calculator.CalculateAsync(dto, session);

            var closing = new ClosingEntity
            {
                ClosingNumber = await GenerateClosingNumberAsync(),
                SalesRepId = session.SalesRepId,
                SalesRepSessionId = session.Id,
                WarehouseId = dto.WarehouseId,
                ClosingDate = DateTime.UtcNow,
                Status = SalesRepDayClosingStatus.Submitted,

                TotalSalesAmount = calculation.TotalSalesAmount,
                TotalCollectionAmount = calculation.TotalCollectionAmount,
                TotalReturnAmount = calculation.TotalReturnAmount,

                ExpectedCashAmount = calculation.ExpectedCashAmount,
                ActualCashAmount = calculation.ActualCashAmount,
                CashVariance = calculation.CashVariance,

                ExpectedTotalRemainingQuantity = calculation.ExpectedTotalRemainingQuantity,
                ActualTotalReturnedQuantity = calculation.ActualTotalReturnedQuantity,
                TotalVarianceQuantity = calculation.TotalVarianceQuantity,

                SubmittedByUserId = _currentUser.UserId,
                SubmittedAt = DateTime.UtcNow,
                Notes = dto.Notes,

                Items = calculation.Items.Select(item => new ClosingItemEntity
                {
                    ProductId = item.ProductId,
                    ExpectedRemainingQuantity = item.ExpectedRemainingQuantity,
                    ActualReturnedQuantity = item.ActualReturnedQuantity,
                    VarianceQuantity = item.VarianceQuantity,
                    Notes = item.Notes
                }).ToList()
            };

            await _unitOfWork.SalesRepDayClosings.AddAsync(closing);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(closing.Id);
        }

        public async Task<SalesRepDayClosingDto> ReceiveReturnedStockAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid closing id.");

            if (_currentUser.Role == "SalesRep")
                throw new UnauthorizedAccessException("Sales reps are not allowed to approve day closings.");

            var closing = await GetClosingForUpdateAsync(id);

            if (closing.Status != SalesRepDayClosingStatus.Submitted)
                throw new InvalidOperationException("Returned stock can only be received for submitted day closing.");

            if (closing.IsStockReceived)
                throw new InvalidOperationException("Returned stock has already been received for this day closing.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _settlementService.ApplyApprovalSettlementAsync(closing);

                closing.IsStockReceived = true;
                closing.StockReceivedByUserId = _currentUser.UserId;
                closing.StockReceivedAt = DateTime.UtcNow;

                var cashReceived = closing.ExpectedCashAmount <= 0 || closing.IsCashReceived;

                if (cashReceived)
                {
                    closing.Status = SalesRepDayClosingStatus.Completed;
                }

                closing.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.SalesRepDayClosings.Update(closing);

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return await GetByIdAsync(id);
        }


        public async Task CancelAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid closing id.");

            var closing = await GetClosingForUpdateAsync(id);

            await EnsureSalesRepCanAccessClosingAsync(closing.SalesRepId);

            if (closing.Status != SalesRepDayClosingStatus.Submitted)
                throw new InvalidOperationException("Only pending day closings can be cancelled.");

            closing.Status = SalesRepDayClosingStatus.Cancelled;
            closing.CancelledByUserId = _currentUser.UserId;
            closing.CancelledAt = DateTime.UtcNow;
            closing.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepDayClosings.Update(closing);
            await _unitOfWork.CompleteAsync();
        }


        #region Helper Methods

        private IQueryable<ClosingEntity> GetClosingQuery()
        {
            return _unitOfWork.SalesRepDayClosings
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.SalesRep)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                .Where(x => x.IsActive);
        }

        private async Task<ClosingEntity> GetClosingForUpdateAsync(int id)
        {
            var closing = await _unitOfWork.SalesRepDayClosings
                .GetQueryable()
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (closing == null)
                throw new KeyNotFoundException("Sales rep day closing not found.");

            return closing;
        }

        private async Task<Salesync.Domain.Modules.Sales.Entities.SalesRepSession> GetSessionForClosingAsync(int salesRepSessionId)
        {
            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == salesRepSessionId &&
                    x.IsActive);

            if (session == null)
                throw new KeyNotFoundException("Sales rep session not found.");

            return session;
        }

        private async Task EnsureWarehouseExistsAsync(int warehouseId)
        {
            var exists = await _unitOfWork.Warehouses
                .GetQueryable()
                .AnyAsync(x => x.Id == warehouseId && x.IsActive);

            if (!exists)
                throw new KeyNotFoundException("Warehouse not found.");
        }

        private async Task EnsureNoClosingExistsForSessionAsync(int salesRepSessionId)
        {
            var exists = await _unitOfWork.SalesRepDayClosings
                .GetQueryable()
                .AnyAsync(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.IsActive &&
                    (
                        x.Status == SalesRepDayClosingStatus.Submitted ||
                        x.Status == SalesRepDayClosingStatus.Completed
                    ));

            if (exists)
                throw new InvalidOperationException("There is already a pending or approved day closing for this session.");
        }

        private async Task EnsureProductsExistAsync(List<int> productIds)
        {
            var distinctProductIds = productIds.Distinct().ToList();

            var existingProductIds = await _unitOfWork.Products
                .GetQueryable()
                .Where(x =>
                    distinctProductIds.Contains(x.Id) &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            var missingProductIds = distinctProductIds
                .Except(existingProductIds)
                .ToList();

            if (missingProductIds.Any())
                throw new KeyNotFoundException($"Products not found: {string.Join(", ", missingProductIds)}");
        }

        private async Task EnsureAllCurrentInventoryProductsProvidedAsync(int salesRepId, CreateSalesRepDayClosingDto dto)
        {
            var currentInventoryProductIds = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.Quantity > 0 &&
                    x.IsActive)
                .Select(x => x.ProductId)
                .ToListAsync();

            var dtoProductIds = dto.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var missingProductIds = currentInventoryProductIds
                .Except(dtoProductIds)
                .ToList();

            if (missingProductIds.Any())
                throw new InvalidOperationException(
                    $"Closing items must include all current inventory products. Missing products: {string.Join(", ", missingProductIds)}");
        }

        private async Task EnsureActualReturnedDoesNotExceedCurrentInventoryAsync(int salesRepId, CreateSalesRepDayClosingDto dto)
        {
            var currentInventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.ProductId,
                    x.Quantity
                })
                .ToListAsync();

            var inventoryByProduct = currentInventory
                .ToDictionary(x => x.ProductId, x => x.Quantity);

            foreach (var item in dto.Items)
            {
                var expectedQuantity = inventoryByProduct.TryGetValue(item.ProductId, out var quantity)
                    ? quantity
                    : 0;

                if (item.ActualReturnedQuantity > expectedQuantity)
                    throw new InvalidOperationException(
                        $"Actual returned quantity for product {item.ProductId} cannot exceed expected remaining quantity. Expected: {expectedQuantity}, Actual: {item.ActualReturnedQuantity}.");
            }
        }

        private async Task EnsureSalesRepCanAccessClosingAsync(int salesRepId)
        {
            if (_currentUser.Role != "SalesRep")
                return;

            var currentSalesRepId = await _unitOfWork.SalesReps
                .GetQueryable()
                .Where(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (currentSalesRepId == 0 || currentSalesRepId != salesRepId)
                throw new UnauthorizedAccessException("You are not allowed to access this day closing.");
        }

        private async Task<IQueryable<ClosingEntity>> ApplySalesRepAccessFilterAsync(IQueryable<ClosingEntity> query)
        {
            if (_currentUser.Role != "SalesRep")
                return query;

            var currentSalesRepId = await _unitOfWork.SalesReps
                .GetQueryable()
                .Where(x =>
                    x.UserId == _currentUser.UserId &&
                    x.IsActive)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (currentSalesRepId == 0)
                throw new UnauthorizedAccessException("Current user is not linked to an active sales rep.");

            return query.Where(x => x.SalesRepId == currentSalesRepId);
        }

        private async Task<string> GenerateClosingNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"DC-{today:yyyyMMdd}";

            var count = await _unitOfWork.SalesRepDayClosings
                .GetQueryable()
                .CountAsync(x => x.ClosingNumber.StartsWith(prefix));

            return $"{prefix}-{count + 1:D4}";
        }

        #endregion
    }
}