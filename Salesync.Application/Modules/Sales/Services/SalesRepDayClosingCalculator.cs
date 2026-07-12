using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Sales.Models;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
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

        public async Task<SalesRepDayClosingCalculationResult> CalculateAsync(
            CreateSalesRepDayClosingDto dto,
            SalesRepSession session)
        {
            var totalSalesAmount = await CalculateTotalSalesAmountAsync(session.Id);

            var totalCollectionAmount = await CalculateTotalCollectionAmountAsync(session.Id);

            var totalReturnAmount = await CalculateTotalReturnAmountAsync(session.Id);

            var items = await CalculateItemsAsync(dto, session.SalesRepId);

            var expectedTotalRemainingQuantity = items.Sum(x => x.ExpectedRemainingQuantity);

            var actualTotalReturnedQuantity = items.Sum(x => x.ActualReturnedQuantity);

            return new SalesRepDayClosingCalculationResult
            {
                TotalSalesAmount = totalSalesAmount,
                TotalCollectionAmount = totalCollectionAmount,
                TotalReturnAmount = totalReturnAmount,

                ExpectedCashAmount = totalCollectionAmount,
                ActualCashAmount = dto.ActualCashAmount,
                CashVariance = dto.ActualCashAmount - totalCollectionAmount,

                ExpectedTotalRemainingQuantity = expectedTotalRemainingQuantity,
                ActualTotalReturnedQuantity = actualTotalReturnedQuantity,
                TotalVarianceQuantity = actualTotalReturnedQuantity - expectedTotalRemainingQuantity,

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

        private async Task<List<SalesRepDayClosingItemCalculation>> CalculateItemsAsync(
            CreateSalesRepDayClosingDto dto,
            int salesRepId)
        {
            var currentInventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.IsActive)
                .ToListAsync();

            var expectedByProduct = currentInventory
                .ToDictionary(x => x.ProductId, x => x.Quantity);

            var result = dto.Items.Select(item =>
            {
                var expectedQuantity = expectedByProduct.TryGetValue(item.ProductId, out var quantity)
                    ? quantity
                    : 0;

                return new SalesRepDayClosingItemCalculation
                {
                    ProductId = item.ProductId,
                    ExpectedRemainingQuantity = expectedQuantity,
                    ActualReturnedQuantity = item.ActualReturnedQuantity,
                    VarianceQuantity = item.ActualReturnedQuantity - expectedQuantity,
                    Notes = item.Notes
                };
            }).ToList();

            return result;
        }
    }
}