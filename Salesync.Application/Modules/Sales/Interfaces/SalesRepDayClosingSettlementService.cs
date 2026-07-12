using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Modules.LoadRequest.Entities;
using ClosingEntity = Salesync.Domain.Modules.Sales.Entities.SalesRepDayClosing;

namespace Salesync.Application.Modules.Sales.Interfaces
{
    public class SalesRepDayClosingSettlementService : ISalesRepDayClosingSettlementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IInventoryService _inventoryService;

        public SalesRepDayClosingSettlementService(
            IUnitOfWork unitOfWork,
            IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _inventoryService = inventoryService;
        }

        public async Task ApplyApprovalSettlementAsync(ClosingEntity closing)
        {
            foreach (var item in closing.Items)
            {
                await DecreaseSalesRepInventoryAsync(
                    closing.SalesRepId,
                    item.ProductId,
                    item.ExpectedRemainingQuantity,
                    item.ActualReturnedQuantity,
                    item.VarianceQuantity,
                    closing.Id,
                    closing.ClosingNumber);

                if (item.ActualReturnedQuantity > 0)
                {
                    await _inventoryService.StockInAsync(
                        item.ProductId,
                        closing.WarehouseId,
                        item.ActualReturnedQuantity,
                        StockMovementSource.StockTransfer,
                        closing.Id,
                        closing.ClosingNumber,
                        "Stock returned from sales rep day closing.");
                }
            }
        }

        private async Task DecreaseSalesRepInventoryAsync(
            int salesRepId,
            int productId,
            int expectedRemainingQuantity,
            int actualReturnedQuantity,
            int varianceQuantity,
            int closingId,
            string closingNumber)
        {
            if (expectedRemainingQuantity <= 0)
                return;

            var inventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.SalesRepId == salesRepId &&
                    x.ProductId == productId &&
                    x.IsActive);

            if (inventory == null)
                throw new InvalidOperationException(
                    $"Sales rep inventory not found for product {productId}.");

            if (inventory.Quantity < expectedRemainingQuantity)
                throw new InvalidOperationException(
                    $"Sales rep inventory changed for product {productId}. Please recalculate closing.");

            inventory.Quantity -= expectedRemainingQuantity;
            inventory.LastUpdatedAt = DateTime.UtcNow;
            inventory.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesRepInventories.Update(inventory);

            var movement = new SalesRepInventoryMovement
            {
                SalesRepId = salesRepId,
                ProductId = productId,
                Quantity = expectedRemainingQuantity,
                MovementType = SalesRepInventoryMovementType.Out,
                Source = SalesRepInventoryMovementSource.EndDayReturn,
                SourceId = closingId,
                SourceNumber = closingNumber,
                MovementDate = DateTime.UtcNow,
                Notes =
                    $"Day closing settlement. Expected: {expectedRemainingQuantity}, Actual returned: {actualReturnedQuantity}, Variance: {varianceQuantity}."
            };

            await _unitOfWork.SalesRepInventoryMovements.AddAsync(movement);
        }
    }
}