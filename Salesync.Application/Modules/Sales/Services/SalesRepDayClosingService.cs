using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Dtos;
using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Common.Enums.UnloadRequest;
using ClosingEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosing;
using ClosingItemEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosingItem;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepDayClosingService : ISalesRepDayClosingService
    {
        private readonly ISalesAccessScopeService _access;
        private readonly ITreasuryService _treasury;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateSalesRepDayClosingDto> _createValidator;
        private readonly ICurrentUserService _currentUser;
        private readonly ISalesRepDayClosingCalculator _calculator;

        public SalesRepDayClosingService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateSalesRepDayClosingDto> createValidator,
            ICurrentUserService currentUser,
            ISalesRepDayClosingCalculator calculator,
            ISalesAccessScopeService access, ITreasuryService treasury)
        {
            _access = access;
            _treasury = treasury;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _currentUser = currentUser;
            _calculator = calculator;
        }

        public async Task<IEnumerable<SalesRepDayClosingDto>> GetAllAsync(SalesRepDayClosingFilterDto filter)
        {
            var accessScope = await _access.GetAsync();
            filter ??= new SalesRepDayClosingFilterDto();

            var query = GetClosingQuery().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId));

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
            var accessScope = await _access.GetAsync();
            if (id <= 0)
                throw new ArgumentException("Invalid closing id.");

            var closing = await GetClosingQuery().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .FirstOrDefaultAsync(x => x.Id == id);

            if (closing == null)
                throw new KeyNotFoundException("Sales rep day closing not found.");

            await EnsureSalesRepCanAccessClosingAsync(closing.SalesRepId);

            return _mapper.Map<SalesRepDayClosingDto>(closing);
        }
        public async Task<IEnumerable<SalesRepDayClosingDto>> GetBySalesRepAsync(int salesRepId)
        {
            await _access.RequireSalesRepAsync(salesRepId);
            var accessScope = await _access.GetAsync();
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            await EnsureSalesRepCanAccessClosingAsync(salesRepId);

            var closings = await GetClosingQuery().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .Where(x => x.SalesRepId == salesRepId)
                .OrderByDescending(x => x.ClosingDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepDayClosingDto>>(closings);
        }
        public Task<SalesRepDayClosingDto> CreateAsync(CreateSalesRepDayClosingDto dto) =>
            _unitOfWork.ExecuteInTransactionAsync(() => CreateAsyncCore(dto));

        private async Task<SalesRepDayClosingDto> CreateAsyncCore(CreateSalesRepDayClosingDto dto)
        {
            var accessScope = await _access.GetAsync();
            var validationResult =
                await _createValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var session =
                await GetSessionForClosingAsync(
                    dto.SalesRepSessionId);

            await EnsureSalesRepCanAccessClosingAsync(
                session.SalesRepId);

            if (session.Status != DayStatus.Closed)
                throw new InvalidOperationException(
                    "Sales rep session must be closed before submitting day closing.");

            if (!session.IsStockSettled)
                throw new InvalidOperationException(
                    "Stock must be fully settled before submitting day closing.");

            if (await _unitOfWork.SalesRepInventories.GetQueryable().AnyAsync(x =>
                x.SalesRepId == session.SalesRepId && x.IsActive && x.Quantity != 0))
                throw new InvalidOperationException("STOCK_REMAINING: Stock must be fully settled.");
            if (await _unitOfWork.SalesRepUnloadRequests.GetQueryable().AnyAsync(x =>
                x.SalesRepSessionId == session.Id && x.IsActive && (x.Status == UnloadRequestStatus.PendingWarehouse || x.Status == UnloadRequestStatus.Draft)))
                throw new InvalidOperationException("PENDING_UNLOAD: Warehouse confirmation is required.");
            await EnsureWarehouseExistsAsync(dto.WarehouseId);

            await EnsureWarehouseMatchesUnloadAsync(session.Id,dto.WarehouseId);

            await EnsureNoClosingExistsForSessionAsync(
                dto.SalesRepSessionId);

            var calculation =
                await _calculator.CalculateAsync(dto, session);

            var requiresCashSettlement =
                calculation.ExpectedCashAmount > 0;

            var closing =
                new ClosingEntity
                {
                    ClosingNumber = await GenerateClosingNumberAsync(),
                    SalesRepId = session.SalesRepId,
                    SalesRepSessionId = session.Id,
                    WarehouseId = dto.WarehouseId,
                    ClosingDate = DateTime.UtcNow,
                    Status = requiresCashSettlement ? SalesRepDayClosingStatus.Submitted : SalesRepDayClosingStatus.Completed,

                    // Sales Summary
                    TotalSalesAmount = calculation.TotalSalesAmount,
                    TotalCollectionAmount = calculation.TotalCollectionAmount,
                    CashCollectionAmount = calculation.CashCollectionAmount,
                    NonCashCollectionAmount = calculation.NonCashCollectionAmount,
                    OutstandingAmount = calculation.OutstandingAmount,
                    TotalReturnAmount = calculation.TotalReturnAmount,

                    // Cash Settlement
                    ExpectedCashAmount = calculation.ExpectedCashAmount,
                    ActualCashAmount = 0,
                    CashVariance = 0,
                    IsCashReceived = false,

                    // Stock was already settled through Unload
                    IsStockReceived = true,
                    StockReceivedAt = session.StockSettledAt,

                    // Stock Summary
                    ExpectedTotalRemainingQuantity = calculation.ExpectedTotalRemainingQuantity,
                    ActualTotalReturnedQuantity = calculation.ActualTotalReturnedQuantity,
                    TotalVarianceQuantity = calculation.TotalVarianceQuantity,

                    // Workflow
                    SubmittedByUserId = _currentUser.UserId,
                    SubmittedAt = DateTime.UtcNow,
                    Notes = dto.Notes,
                    Items =
                        calculation.Items
                            .Select(item =>
                                new ClosingItemEntity
                                {
                                    ProductId =
                                        item.ProductId,

                                    ExpectedRemainingQuantity =
                                        item.ExpectedRemainingQuantity,

                                    ActualReturnedQuantity =
                                        item.ActualReturnedQuantity,

                                    VarianceQuantity =
                                        item.VarianceQuantity,

                                    Notes =
                                        item.Notes
                                })
                            .ToList()
                };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                await _unitOfWork.SalesRepDayClosings
                    .AddAsync(closing);

                if (!requiresCashSettlement)
                {
                    session.IsTreasurySettled = true;
                    session.TreasurySettledAt = DateTime.UtcNow;
                    session.UpdatedAt = DateTime.UtcNow;

                    _unitOfWork.SalesRepSessions.Update(session);
                }

                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            return await GetByIdAsync(closing.Id);
        }
        public async Task<SalesRepDayClosingDto> ReceiveCashAsync(int id, ReceiveSalesRepDayClosingCashDto dto)
        {
            if (!dto.CashBoxId.HasValue || dto.CashBoxId.Value <= 0)
                throw new ArgumentException("CASHBOX_REQUIRED: Select an authorized cash box or use the Treasury receiving endpoint.");
            await _treasury.ReceiveDayClosingCashAsync(id, new ReceiveDayClosingCashDto {
                CashBoxId = dto.CashBoxId.Value, ReceivedAmount = dto.ActualCashAmount, Notes = dto.Notes });
            return await GetByIdAsync(id);
        }
        public Task CancelAsync(int id) =>
            _unitOfWork.ExecuteInTransactionAsync(() => CancelAsyncCore(id));

        private async Task CancelAsyncCore(int id)
        {
            var accessScope = await _access.GetAsync();
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
            var accessScope = await _access.GetAsync();
            var closing = await _unitOfWork.SalesRepDayClosings
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (closing == null)
                throw new KeyNotFoundException("Sales rep day closing not found.");

            return closing;
        }

        private async Task<Salesync.Domain.Modules.Sales.Entities.SalesRepSession> GetSessionForClosingAsync(int salesRepSessionId)
        {
            var accessScope = await _access.GetAsync();
            var session = await _unitOfWork.SalesRepSessions
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .FirstOrDefaultAsync(x =>
                    x.Id == salesRepSessionId &&
                    x.IsActive);

            if (session == null)
                throw new KeyNotFoundException("Sales rep session not found.");

            return session;
        }

        private async Task EnsureWarehouseExistsAsync(int warehouseId)
        {
            await _access.RequireWarehouseAsync(warehouseId);
            var accessScope = await _access.GetAsync();
            var exists = await _unitOfWork.Warehouses
                .GetQueryable()
                .AnyAsync(x => x.Id == warehouseId && x.IsActive);

            if (!exists)
                throw new KeyNotFoundException("Warehouse not found.");
        }

        private async Task EnsureNoClosingExistsForSessionAsync(int salesRepSessionId)
        {
            var accessScope = await _access.GetAsync();
            var exists = await _unitOfWork.SalesRepDayClosings
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .AnyAsync(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.IsActive &&
                    (
                        x.Status == SalesRepDayClosingStatus.Submitted ||
                        x.Status == SalesRepDayClosingStatus.Completed
                    ));

            if (exists)
                throw new InvalidOperationException("There is already a submitted or completed day closing for this session.");
        }

        private async Task EnsureSalesRepCanAccessClosingAsync(int salesRepId)
        {
            await _access.RequireSalesRepAsync(salesRepId);
            var accessScope = await _access.GetAsync();
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
            var accessScope = await _access.GetAsync();
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
            var accessScope = await _access.GetAsync();
            var today = DateTime.UtcNow;
            var prefix = $"DC-{today:yyyyMMdd}";

            var count = await _unitOfWork.SalesRepDayClosings
                .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                .CountAsync(x => x.ClosingNumber.StartsWith(prefix));

            return $"{prefix}-{count + 1:D4}";
        }

        private async Task EnsureWarehouseMatchesUnloadAsync(
    int salesRepSessionId,
    int warehouseId)
        {
            var accessScope = await _access.GetAsync();
            var unloadWarehouseIds =
                await _unitOfWork.SalesRepUnloadRequests
                    .GetQueryable().Where(x => accessScope.AllowsAll || accessScope.SalesRepIds.Contains(x.SalesRepId))
                    .AsNoTracking()
                    .Where(x =>
                        x.SalesRepSessionId == salesRepSessionId &&
                        x.IsActive &&
                        (
                            x.Status == UnloadRequestStatus.Confirmed ||
                            x.Status == UnloadRequestStatus.PartiallyConfirmed
                        ))
                    .Select(x => x.WarehouseId)
                    .Distinct()
                    .ToListAsync();

            if (unloadWarehouseIds.Count == 0) return; // Empty-stock day: warehouse was explicitly authorized.

            if (unloadWarehouseIds.Count > 1)
                throw new InvalidOperationException(
                    "Day closing cannot be created because the session was unloaded to multiple warehouses.");

            if (unloadWarehouseIds[0] != warehouseId)
                throw new InvalidOperationException(
                    "Day closing warehouse must match the warehouse used for stock unload.");
        }

        #endregion
    }
}