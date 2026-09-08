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
            ISalesRepDayClosingCalculator calculator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _currentUser = currentUser;
            _calculator = calculator;
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
            if (id <= 0)
                throw new ArgumentException(
                    "Invalid closing id.");

            if (dto.ActualCashAmount < 0)
                throw new ArgumentException(
                    "Actual cash amount cannot be negative.");

            if (_currentUser.Role != "Admin" &&
                 _currentUser.Role != "Treasury")
            {
                throw new UnauthorizedAccessException(
                    "Only Admin or Treasury users can receive closing cash.");
            }

            var closing =
                await GetClosingForUpdateAsync(id);

            if (closing.Status !=
                SalesRepDayClosingStatus.Submitted)
            {
                throw new InvalidOperationException(
                    "Cash can only be received for submitted day closing.");
            }

            if (closing.IsCashReceived)
                throw new InvalidOperationException(
                    "Cash has already been received for this day closing.");

            if (closing.ExpectedCashAmount <= 0)
                throw new InvalidOperationException(
                    "This day closing has no expected cash amount to receive.");

            var session =
                await _unitOfWork.SalesRepSessions
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == closing.SalesRepSessionId &&
                        x.IsActive);

            if (session == null)
                throw new KeyNotFoundException(
                    "Sales rep session not found.");

            if (!session.IsStockSettled)
                throw new InvalidOperationException(
                    "Stock settlement must be completed before cash settlement.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                closing.ActualCashAmount =
                    dto.ActualCashAmount;

                closing.CashVariance =
                    dto.ActualCashAmount -
                    closing.ExpectedCashAmount;

                closing.CashNotes =
                    string.IsNullOrWhiteSpace(dto.Notes)
                        ? null
                        : dto.Notes.Trim();

                closing.IsCashReceived = true;

                closing.CashReceivedByUserId =
                    _currentUser.UserId;

                closing.CashReceivedAt =
                    DateTime.UtcNow;

                closing.Status =
                    SalesRepDayClosingStatus.Completed;

                closing.UpdatedAt =
                    DateTime.UtcNow;

                session.IsTreasurySettled = true;

                session.TreasurySettledAt =
                    DateTime.UtcNow;

                session.UpdatedAt =
                    DateTime.UtcNow;

                _unitOfWork.SalesRepDayClosings
                    .Update(closing);

                _unitOfWork.SalesRepSessions
                    .Update(session);

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
                throw new InvalidOperationException("There is already a submitted or completed day closing for this session.");
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

        private async Task EnsureWarehouseMatchesUnloadAsync(
    int salesRepSessionId,
    int warehouseId)
        {
            var unloadWarehouseIds =
                await _unitOfWork.SalesRepUnloadRequests
                    .GetQueryable()
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

            if (unloadWarehouseIds.Count == 0)
                throw new InvalidOperationException(
                    "No confirmed unload request found for this session.");

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