using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Treasury;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Application.Modules.Treasury.Services
{
    public class TreasuryService : ITreasuryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly IValidator<CreateCashBoxDto> _createCashBoxValidator;
        private readonly IValidator<ReceiveDayClosingCashDto> _receiveCashValidator;

        public TreasuryService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICurrentUserService currentUser,
            IValidator<CreateCashBoxDto> createCashBoxValidator,
            IValidator<ReceiveDayClosingCashDto> receiveCashValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _createCashBoxValidator = createCashBoxValidator;
            _receiveCashValidator = receiveCashValidator;
        }

        public async Task<IEnumerable<CashBoxDto>> GetCashBoxesAsync()
        {
            var cashBoxes = await _unitOfWork.CashBoxes
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CashBoxDto>>(cashBoxes);
        }

        public async Task<CashBoxDto> GetCashBoxByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid cash box id.");

            var cashBox = await _unitOfWork.CashBoxes
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);

            if (cashBox == null)
                throw new KeyNotFoundException($"Cash box with id {id} not found.");

            return _mapper.Map<CashBoxDto>(cashBox);
        }

        public async Task<CashBoxDto> CreateCashBoxAsync(CreateCashBoxDto dto)
        {
            var validationResult = await _createCashBoxValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var codeExists = await _unitOfWork.CashBoxes
                .GetQueryable()
                .AnyAsync(x => x.Code == dto.Code);

            if (codeExists)
                throw new InvalidOperationException("Cash box code already exists.");

            if (dto.BranchId.HasValue)
            {
                var branchExists = await _unitOfWork.Branches
                    .GetQueryable()
                    .AnyAsync(x => x.Id == dto.BranchId.Value && x.IsActive);

                if (!branchExists)
                    throw new KeyNotFoundException($"Branch with id {dto.BranchId.Value} not found.");
            }

            var cashBox = new CashBox
            {
                Code = dto.Code.Trim(),
                Name = dto.Name.Trim(),
                BranchId = dto.BranchId,
                Currency = dto.Currency.Trim(),
                CurrentBalance = 0,
                Notes = dto.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CashBoxes.AddAsync(cashBox);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CashBoxDto>(cashBox);
        }

        public async Task<CashReceiptDto> ReceiveDayClosingCashAsync(int dayClosingId,ReceiveDayClosingCashDto dto)
        {
            if (dayClosingId <= 0)
                throw new ArgumentException("Invalid day closing id.");

            var validationResult = await _receiveCashValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var userId = _currentUser.UserId;

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("User is not authenticated.");

            var closing = await _unitOfWork.SalesRepDayClosings
                .GetQueryable()
                .Include(x => x.SalesRepSession)
                .FirstOrDefaultAsync(x => x.Id == dayClosingId && x.IsActive);

            if (closing == null)
                throw new KeyNotFoundException($"Day closing with id {dayClosingId} not found.");

            if (closing.Status != SalesRepDayClosingStatus.Submitted)
                throw new InvalidOperationException("Cash can only be received for submitted day closing.");
            if (closing.IsCashReceived)
                throw new InvalidOperationException("Cash has already been received for this day closing.");

            var cashBox = await _unitOfWork.CashBoxes
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == dto.CashBoxId && x.IsActive);

            if (cashBox == null)
                throw new KeyNotFoundException($"Cash box with id {dto.CashBoxId} not found.");

            var hasReceipt = await _unitOfWork.CashReceipts
                .GetQueryable()
                .AnyAsync(x => x.SalesRepDayClosingId == dayClosingId && x.IsActive);

            if (hasReceipt)
                throw new InvalidOperationException("Cash receipt already exists for this day closing.");

            var expectedAmount = closing.ExpectedCashAmount;
            var receivedAmount = dto.ReceivedAmount;
            var varianceAmount = receivedAmount - expectedAmount;

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var receipt = new CashReceipt
                {
                    ReceiptNumber = GenerateReceiptNumber(),
                    CashBoxId = cashBox.Id,
                    SalesRepId = closing.SalesRepId,
                    SalesRepSessionId = closing.SalesRepSessionId,
                    SalesRepDayClosingId = closing.Id,
                    ExpectedAmount = expectedAmount,
                    ReceivedAmount = receivedAmount,
                    VarianceAmount = varianceAmount,
                    ReceivedByUserId = userId,
                    ReceivedAt = DateTime.UtcNow,
                    Notes = dto.Notes,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.CashReceipts.AddAsync(receipt);

                cashBox.CurrentBalance += receivedAmount;
                cashBox.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.CashBoxes.Update(cashBox);

                await _unitOfWork.CompleteAsync();

                if (varianceAmount != 0)
                {
                    var currentSalesRepCashBalance = await GetCurrentSalesRepCashBalanceAsync(closing.SalesRepId);

                    var ledger = new SalesRepCashLedger
                    {
                        SalesRepId = closing.SalesRepId,
                        EntryDate = DateTime.UtcNow,
                        Amount = varianceAmount,
                        BalanceAfter = currentSalesRepCashBalance + varianceAmount,
                        Source = SalesRepCashLedgerSource.CashReceipt,
                        CashReceiptId = receipt.Id,
                        ReferenceNumber = receipt.ReceiptNumber,
                        Notes = BuildVarianceNotes(expectedAmount, receivedAmount, varianceAmount),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.SalesRepCashLedgers.AddAsync(ledger);
                }

                closing.ActualCashAmount = receivedAmount;
                closing.CashVariance = varianceAmount;
                closing.IsCashReceived = true;
                closing.CashReceivedByUserId = userId;
                closing.CashReceivedAt = DateTime.UtcNow;
                closing.CashNotes = dto.Notes;

                if (closing.IsStockReceived)
                {
                    closing.Status = SalesRepDayClosingStatus.Completed;
                }

                closing.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.SalesRepDayClosings.Update(closing);

                await _unitOfWork.CompleteAsync();
                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<CashReceiptDto>(receipt);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<IEnumerable<SalesRepCashLedgerDto>> GetSalesRepLedgerAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            var ledger = await _unitOfWork.SalesRepCashLedgers
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                .OrderByDescending(x => x.EntryDate)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepCashLedgerDto>>(ledger);
        }

        public async Task<decimal> GetSalesRepCashBalanceAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            return await GetCurrentSalesRepCashBalanceAsync(salesRepId);
        }

        private async Task<decimal> GetCurrentSalesRepCashBalanceAsync(int salesRepId)
        {
            return await _unitOfWork.SalesRepCashLedgers
                .GetQueryable()
                .Where(x => x.SalesRepId == salesRepId && x.IsActive)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;
        }

        private static string GenerateReceiptNumber()
        {
            return $"CR-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        }

        private static string BuildVarianceNotes(decimal expectedAmount,decimal receivedAmount,decimal varianceAmount)
        {
            if (varianceAmount < 0)
            {
                return $"Cash shortage. Expected: {expectedAmount}, Received: {receivedAmount}, Variance: {varianceAmount}.";
            }

            return $"Cash surplus. Expected: {expectedAmount}, Received: {receivedAmount}, Variance: {varianceAmount}.";
        }

    }
}
