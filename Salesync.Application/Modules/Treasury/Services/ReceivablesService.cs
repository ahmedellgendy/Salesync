using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Treasury.Dtos.Receivables;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;

namespace Salesync.Application.Modules.Treasury.Services
{
    public class ReceivablesService
        : IReceivablesService
    {
        private readonly IUnitOfWork _unitOfWork;


        public ReceivablesService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork =
                unitOfWork;
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

        public async Task<IEnumerable<CustomerReceivableDto>>
            GetCustomerReceivablesAsync()
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

        public async Task<IEnumerable<CustomerReceivableInvoiceDto>>
            GetCustomerReceivableDetailsAsync(
                int customerId)
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

        public async Task<IEnumerable<SalesRepReceivableDto>>
            GetSalesRepReceivablesAsync()
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
    }
}