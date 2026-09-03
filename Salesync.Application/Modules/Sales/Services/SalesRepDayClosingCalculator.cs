using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Sales.Models;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Common.Enums.UnloadRequest;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class SalesRepDayClosingCalculator : ISalesRepDayClosingCalculator
    {
        private readonly IUnitOfWork _unitOfWork;

        public SalesRepDayClosingCalculator(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SalesRepDayClosingCalculationResult> CalculateAsync(CreateSalesRepDayClosingDto dto, SalesRepSession session)
        {
            var totalSalesAmount =
                await CalculateTotalSalesAmountAsync(session.Id);

            var totalCollectionAmount =
                await CalculateTotalCollectionAmountAsync(session.Id);

            var cashCollectionAmount =
                await CalculateExpectedCashAmountAsync(session.Id);

            var nonCashCollectionAmount =
                totalCollectionAmount - cashCollectionAmount;

            var outstandingAmount =
                await CalculateOutstandingAmountAsync(session.Id);

            var totalReturnAmount =
                await CalculateTotalReturnAmountAsync(session.Id);

            var items =
                      await CalculateItemsAsync(session.Id);

            var expectedTotalRemainingQuantity =
                items.Sum(x => x.ExpectedRemainingQuantity);

            var actualTotalReturnedQuantity =
                items.Sum(x => x.ActualReturnedQuantity);

            return new SalesRepDayClosingCalculationResult
            {
                TotalSalesAmount = totalSalesAmount,

                TotalCollectionAmount = totalCollectionAmount,
                CashCollectionAmount = cashCollectionAmount,
                NonCashCollectionAmount = nonCashCollectionAmount,
                OutstandingAmount = outstandingAmount,

                TotalReturnAmount = totalReturnAmount,

                ExpectedCashAmount = cashCollectionAmount,
                ActualCashAmount = 0,
                CashVariance = 0,

                ExpectedTotalRemainingQuantity =
                    expectedTotalRemainingQuantity,

                ActualTotalReturnedQuantity =
                    actualTotalReturnedQuantity,

                TotalVarianceQuantity =
                    actualTotalReturnedQuantity -
                    expectedTotalRemainingQuantity,

                Items = items
            };
        }

        private async Task<decimal> CalculateTotalSalesAmountAsync(int salesRepSessionId)
        {
            return await _unitOfWork.Invoices
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.IsActive)
                .SumAsync(x => x.TotalAmount);
        }

        private async Task<decimal> CalculateTotalCollectionAmountAsync(int salesRepSessionId)
        {
            return await _unitOfWork.Payments
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.Status == PaymentStatus.Paid &&
                    x.IsActive)
                .SumAsync(x => x.Amount);
        }

        private async Task<decimal> CalculateTotalReturnAmountAsync(int salesRepSessionId)
        {
            return await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.Status == ReturnStatus.Approved &&
                    x.IsActive)
                .SumAsync(x => x.TotalAmount);
        }

        private async Task<List<SalesRepDayClosingItemCalculation>>CalculateItemsAsync(int salesRepSessionId)
        {
            var unloadItems =
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
                    .SelectMany(x =>
                        x.Items.Where(i => i.IsActive))
                    .GroupBy(x => x.ProductId)
                    .Select(g => new
                    {
                        ProductId = g.Key,

                        // أعلى BeforeUnload يمثل الكمية الأصلية
                        // قبل بداية سلسلة التفريغ
                        ExpectedQuantity =
                            g.Max(x => x.SalesRepInventoryBeforeUnload),

                        // إجمالي ما استلمه المخزن فعلياً
                        ConfirmedQuantity =
                            g.Sum(x => x.ConfirmedQuantity)
                    })
                    .ToListAsync();

            return unloadItems
                .Select(x =>
                    new SalesRepDayClosingItemCalculation
                    {
                        ProductId =
                            x.ProductId,

                        ExpectedRemainingQuantity =
                            x.ExpectedQuantity,

                        ActualReturnedQuantity =
                            x.ConfirmedQuantity,

                        VarianceQuantity =
                            x.ConfirmedQuantity -
                            x.ExpectedQuantity,

                        Notes = null
                    })
                .ToList();
        }

        private async Task<decimal> CalculateExpectedCashAmountAsync(int salesRepSessionId)
        {
            return await _unitOfWork.Payments
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.Status == PaymentStatus.Paid &&
                    x.PaymentMethod == PaymentMethod.Cash &&
                    x.IsActive)
                .SumAsync(x => x.Amount);
        }

        private async Task<decimal> CalculateOutstandingAmountAsync(int salesRepSessionId)
        {
            return await _unitOfWork.Invoices
                .GetQueryable()
                .Where(x =>
                    x.SalesRepSessionId == salesRepSessionId &&
                    x.Status == InvoiceStatus.Confirmed &&
                    x.IsActive)
                .SumAsync(x => x.TotalAmount - x.PaidAmount);
        }

    }
}