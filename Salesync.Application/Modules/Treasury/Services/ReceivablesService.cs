using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Dtos.Receivables;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Treasury;
using Salesync.Domain.Modules.Treasury.Entities;

namespace Salesync.Application.Modules.Treasury.Services
{
    public class ReceivablesService
        : IReceivablesService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;


        public ReceivablesService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)

        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;

        }


        // =========================================================
        // Summary
        // =========================================================

        public async Task<ReceivablesSummaryDto>
            GetSummaryAsync()
        {
            var customers =
                await GetCustomerReceivablesAsync();

            var salesReps =
                await GetSalesRepReceivablesAsync();


            var customerList =
                customers.ToList();

            var salesRepList =
                salesReps.ToList();


            return new ReceivablesSummaryDto
            {
                CustomerDebtAmount =
                    customerList.Sum(x =>
                        x.OutstandingAmount),

                SalesRepDebtAmount =
                    salesRepList.Sum(x =>
                        x.DebtAmount),

                SalesRepSurplusAmount =
                    salesRepList.Sum(x =>
                        x.SurplusAmount),

                CustomersWithDebt =
                    customerList.Count(x =>
                        x.OutstandingAmount > 0),

                SalesRepsWithDebt =
                    salesRepList.Count(x =>
                        x.DebtAmount > 0)
            };
        }


        // =========================================================
        // Customer Receivables
        // =========================================================

        public async Task<IEnumerable<CustomerReceivableDto>> GetCustomerReceivablesAsync()
        {
            var invoices =
                await _unitOfWork.Invoices
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(x => x.Customer)
                    .Where(x =>
                        x.IsActive &&
                        x.Status ==
                            InvoiceStatus.Confirmed)
                    .Select(x =>
                        new
                        {
                            x.CustomerId,

                            CustomerName =
                                x.Customer.Name,

                            AccountNumber =
                                x.Customer.AccountNumber,

                            x.TotalAmount,

                            x.PaidAmount
                        })
                    .ToListAsync();


            var result =
                invoices
                    .GroupBy(x =>
                        new
                        {
                            x.CustomerId,
                            x.CustomerName,
                            x.AccountNumber
                        })
                    .Select(group =>
                    {
                        var totalInvoiceAmount =
                            group.Sum(x =>
                                x.TotalAmount);

                        var totalPaidAmount =
                            group.Sum(x =>
                                x.PaidAmount);

                        var outstanding =
                            Math.Max(
                                0m,
                                totalInvoiceAmount -
                                totalPaidAmount);


                        return new CustomerReceivableDto
                        {
                            CustomerId =
                                group.Key.CustomerId,

                            CustomerName =
                                group.Key.CustomerName,

                            AccountNumber =
                                group.Key.AccountNumber,

                            TotalInvoices =
                                group.Count(),

                            TotalInvoiceAmount =
                                totalInvoiceAmount,

                            TotalPaidAmount =
                                totalPaidAmount,

                            OutstandingAmount =
                                outstanding
                        };
                    })
                    .Where(x =>
                        x.OutstandingAmount > 0)
                    .OrderByDescending(x =>
                        x.OutstandingAmount)
                    .ThenBy(x =>
                        x.CustomerName)
                    .ToList();


            return result;
        }


        // =========================================================
        // Customer Receivable Details
        // =========================================================

        public async Task<IEnumerable<CustomerReceivableInvoiceDto>> GetCustomerReceivableDetailsAsync(int customerId)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException(
                    "Invalid customer id.");
            }


            var customerExists =
                await _unitOfWork.Customers
                    .GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Id == customerId &&
                        x.IsActive);


            if (!customerExists)
            {
                throw new KeyNotFoundException(
                    $"Customer with id {customerId} not found.");
            }


            var invoices =
                await _unitOfWork.Invoices
                    .GetQueryable()
                    .AsNoTracking()
                    .Include(x => x.SalesRep)
                    .Where(x =>
                        x.CustomerId == customerId &&
                        x.IsActive &&
                        x.Status ==
                            InvoiceStatus.Confirmed)
                    .OrderByDescending(x =>
                        x.CreatedAt)
                    .ToListAsync();


            return invoices
                .Select(x =>
                {
                    var outstanding =
                        Math.Max(
                            0m,
                            x.TotalAmount -
                            x.PaidAmount);


                    return new CustomerReceivableInvoiceDto
                    {
                        InvoiceId =
                            x.Id,

                        InvoiceNumber =
                            x.InvoiceNumber,

                        InvoiceDate =
                            x.CreatedAt,

                        TotalAmount =
                            x.TotalAmount,

                        PaidAmount =
                            x.PaidAmount,

                        OutstandingAmount =
                            outstanding,

                        PaymentStatus =
                            (int)x.PaymentStatus,

                        SalesRepId =
                            x.SalesRepId,

                        SalesRepName =
                            x.SalesRep?.Name
                    };
                })
                .Where(x =>
                    x.OutstandingAmount > 0)
                .OrderByDescending(x =>
                    x.InvoiceDate)
                .ToList();
        }


        // =========================================================
        // Sales Rep Receivables
        // =========================================================

        public async Task<IEnumerable<SalesRepReceivableDto>> GetSalesRepReceivablesAsync()
        {
            var salesReps =
                await _unitOfWork.SalesReps
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive)
                    .Select(x =>
                        new
                        {
                            x.Id,
                            x.SalesRepCode,
                            x.Name
                        })
                    .ToListAsync();


            var ledgerBalances =
                await _unitOfWork
                    .SalesRepCashLedgers
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive)
                    .GroupBy(x =>
                        x.SalesRepId)
                    .Select(group =>
                        new
                        {
                            SalesRepId =
                                group.Key,

                            Balance =
                                group.Sum(x =>
                                    x.Amount)
                        })
                    .ToDictionaryAsync(
                        x => x.SalesRepId,
                        x => x.Balance);


            var result =
                salesReps
                    .Select(salesRep =>
                    {
                        ledgerBalances.TryGetValue(
                            salesRep.Id,
                            out var balance);


                        var debt =
                            balance < 0
                                ? Math.Abs(balance)
                                : 0m;


                        var surplus =
                            balance > 0
                                ? balance
                                : 0m;


                        return new SalesRepReceivableDto
                        {
                            SalesRepId =
                                salesRep.Id,

                            SalesRepCode =
                                salesRep.SalesRepCode,

                            SalesRepName =
                                salesRep.Name,

                            LedgerBalance =
                                balance,

                            DebtAmount =
                                debt,

                            SurplusAmount =
                                surplus
                        };
                    })
                    .Where(x =>
                        x.DebtAmount > 0 ||
                        x.SurplusAmount > 0)
                    .OrderByDescending(x =>
                        x.DebtAmount)
                    .ThenBy(x =>
                        x.SalesRepName)
                    .ToList();


            return result;
        }


        public async Task<SalesRepDebtPaymentResultDto> PaySalesRepDebtAsync(int salesRepId, PaySalesRepDebtDto dto)
        {
            if (salesRepId <= 0)
                throw new ArgumentException(
                    "Invalid sales rep id.");

            if (dto.CashBoxId <= 0)
                throw new ArgumentException(
                    "Invalid cash box id.");

            if (dto.Amount <= 0)
                throw new InvalidOperationException(
                    "Payment amount must be greater than zero.");

            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException(
                    "User is not authenticated.");


            var salesRep =
                await _unitOfWork.SalesReps
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == salesRepId &&
                        x.IsActive);

            if (salesRep == null)
                throw new KeyNotFoundException(
                    $"Sales rep with id {salesRepId} not found.");


            var currentBalance =
                await _unitOfWork
                    .SalesRepCashLedgers
                    .GetQueryable()
                    .Where(x =>
                        x.SalesRepId == salesRepId &&
                        x.IsActive)
                    .SumAsync(x =>
                        (decimal?)x.Amount)
                ?? 0m;


            if (currentBalance >= 0)
                throw new InvalidOperationException(
                    "Sales rep has no outstanding debt.");


            var currentDebt =
                Math.Abs(currentBalance);


            if (dto.Amount > currentDebt)
            {
                throw new InvalidOperationException(
                    $"Payment amount exceeds current debt of {currentDebt:N2}.");
            }


            var cashBox =
                await _unitOfWork.CashBoxes
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.CashBoxId &&
                        x.IsActive);

            if (cashBox == null)
                throw new KeyNotFoundException(
                    $"Cash box with id {dto.CashBoxId} not found.");


            var now =
                DateTime.UtcNow;

            var cashBoxBalanceBefore =
                cashBox.CurrentBalance;

            var cashBoxBalanceAfter =
                cashBoxBalanceBefore +
                dto.Amount;

            var newLedgerBalance =
                currentBalance +
                dto.Amount;


            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var treasuryTransaction =
                    new TreasuryTransaction
                    {
                        CashBoxId =
                            cashBox.Id,

                        Type =
                            TreasuryTransactionType.CashIn,

                        Source =
                            TreasuryTransactionSource.SalesRepSettlement,

                        Amount =
                            dto.Amount,

                        BalanceBefore =
                            cashBoxBalanceBefore,

                        BalanceAfter =
                            cashBoxBalanceAfter,

                        TransactionDate =
                            now,

                        ReferenceNumber =
                            string.IsNullOrWhiteSpace(
                                dto.ReferenceNumber)
                                ? null
                                : dto.ReferenceNumber.Trim(),

                        Notes =
                            string.IsNullOrWhiteSpace(
                                dto.Notes)
                                ? $"Sales rep debt payment. SalesRepId: {salesRepId}."
                                : dto.Notes.Trim(),

                        CreatedByUserId =
                            _currentUser.UserId,

                        IsActive =
                            true,

                        CreatedAt =
                            now
                    };


                await _unitOfWork
                    .TreasuryTransactions
                    .AddAsync(treasuryTransaction);


                cashBox.CurrentBalance =
                    cashBoxBalanceAfter;

                cashBox.UpdatedAt =
                    now;

                _unitOfWork.CashBoxes
                    .Update(cashBox);


                var ledgerEntry =
                    new SalesRepCashLedger
                    {
                        SalesRepId =
                            salesRepId,

                        EntryDate =
                            now,

                        Amount =
                            dto.Amount,

                        BalanceAfter =
                            newLedgerBalance,

                        Source =
                            SalesRepCashLedgerSource.DebtPayment,

                        ReferenceNumber =
                            string.IsNullOrWhiteSpace(
                                dto.ReferenceNumber)
                                ? null
                                : dto.ReferenceNumber.Trim(),

                        Notes =
                            string.IsNullOrWhiteSpace(
                                dto.Notes)
                                ? $"Debt payment of {dto.Amount:N2}."
                                : dto.Notes.Trim(),

                        IsActive =
                            true,

                        CreatedAt =
                            now
                    };


                await _unitOfWork
                    .SalesRepCashLedgers
                    .AddAsync(ledgerEntry);


                await _unitOfWork
                    .CompleteAsync();

                await _unitOfWork
                    .CommitTransactionAsync();


                return new SalesRepDebtPaymentResultDto
                {
                    SalesRepId =
                        salesRep.Id,

                    SalesRepName =
                        salesRep.Name,

                    PreviousDebtAmount =
                        currentDebt,

                    PaidAmount =
                        dto.Amount,

                    RemainingDebtAmount =
                        Math.Abs(
                            Math.Min(
                                0m,
                                newLedgerBalance)),

                    CashBoxId =
                        cashBox.Id,

                    CashBoxBalanceAfter =
                        cashBox.CurrentBalance,

                    TreasuryTransactionId =
                        treasuryTransaction.Id,

                    PaymentDate =
                        now
                };
            }
            catch
            {
                await _unitOfWork
                    .RollbackTransactionAsync();

                throw;
            }
        }
    }
}