using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.LoadRequest;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class InvoiceReturnService : IInvoiceReturnService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvoiceReturnService(IUnitOfWork unitOfWork, IMapper mapper, IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InvoiceReturnDto>> GetByInvoiceIdAsync(int invoiceId)
        {
            var returns = await _unitOfWork.InvoiceReturns.FindAsync(r => r.InvoiceId == invoiceId);
            return _mapper.Map<IEnumerable<InvoiceReturnDto>>(returns);
        }
        public async Task<InvoiceReturnDto> CreateAsync(CreateInvoiceReturnDto dto)
        {
            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId);

            if (invoice == null)
                throw new KeyNotFoundException($"Invoice with id {dto.InvoiceId} not found.");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Returns are allowed only for confirmed invoices.");

            if (!invoice.InvoiceItems.Any())
                throw new InvalidOperationException("Cannot create return for invoice without items.");

            var invoiceReturn = _mapper.Map<InvoiceReturn>(dto);

            invoiceReturn.ReturnNumber = GenerateReturnNumber();
            invoiceReturn.CustomerId = invoice.CustomerId;
            invoiceReturn.SalesRepId = invoice.SalesRepId;
            invoiceReturn.SalesRepSessionId = invoice.SalesRepSessionId;
            invoiceReturn.Status = ReturnStatus.Pending;
            invoiceReturn.CreatedAt = DateTime.UtcNow;
            invoiceReturn.IsActive = true;

            invoiceReturn.Items.Clear();

            foreach (var itemDto in dto.Items)
            {
                var invoiceItem = invoice.InvoiceItems
                    .FirstOrDefault(i => i.Id == itemDto.InvoiceItemId);

                if (invoiceItem == null)
                    throw new InvalidOperationException($"Invoice item with id {itemDto.InvoiceItemId} does not belong to this invoice.");

                if (itemDto.Quantity <= 0)
                    throw new InvalidOperationException("Return quantity must be greater than zero.");

                if (itemDto.Quantity > invoiceItem.Quantity)
                    throw new InvalidOperationException("Return quantity cannot be greater than sold quantity.");

                var returnItem = new InvoiceReturnItem
                {
                    InvoiceItemId = invoiceItem.Id,
                    ProductId = invoiceItem.ProductId,
                    ProductName = invoiceItem.ProductName,
                    ItemCode = invoiceItem.ItemCode,
                    Quantity = itemDto.Quantity,
                    UnitPrice = invoiceItem.UnitPrice,
                    TotalAmount = itemDto.Quantity * invoiceItem.UnitPrice,
                    Notes = itemDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                invoiceReturn.Items.Add(returnItem);
            }

            invoiceReturn.TotalAmount = invoiceReturn.Items.Sum(i => i.TotalAmount);


            await _unitOfWork.InvoiceReturns.AddAsync(invoiceReturn);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
        }
        public async Task<InvoiceReturnDto> ApproveAsync(int id)
        {
            var invoiceReturn = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (invoiceReturn == null)
                throw new KeyNotFoundException($"Return with id {id} not found.");

            if (!invoiceReturn.IsActive)
                throw new InvalidOperationException("Cannot approve inactive return.");

            if (invoiceReturn.Status != ReturnStatus.Pending)
                throw new InvalidOperationException("Only pending returns can be approved.");

            if (!invoiceReturn.Items.Any())
                throw new InvalidOperationException("Cannot approve return without items.");

            if (invoiceReturn.TotalAmount <= 0)
                throw new InvalidOperationException("Cannot approve return with invalid total amount.");

            var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceReturn.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice with id {invoiceReturn.InvoiceId} not found.");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Cannot approve return for unconfirmed invoice.");

            var salesRepId = invoiceReturn.SalesRepId
                ?? invoice.SalesRepId
                ?? throw new InvalidOperationException("Sales rep is required for invoice return.");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in invoiceReturn.Items)
                {
                    if (item.Quantity <= 0)
                        throw new InvalidOperationException($"Invalid return quantity for product {item.ProductName}.");

                    await IncreaseSalesRepInventoryForReturnAsync(
                        salesRepId,
                        item.ProductId,
                        item.Quantity,
                        invoiceReturn.Id,
                        invoiceReturn.ReturnNumber);
                }

                invoiceReturn.Status = ReturnStatus.Approved;
                invoiceReturn.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.InvoiceReturns.Update(invoiceReturn);
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<InvoiceReturnDto> RejectAsync(int id)
        {
            var invoiceReturn = await _unitOfWork.InvoiceReturns.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Return with id {id} not found.");

            if (invoiceReturn.Status != ReturnStatus.Pending)
                throw new InvalidOperationException("Only pending returns can be rejected.");

            invoiceReturn.Status = ReturnStatus.Rejected;
            invoiceReturn.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.InvoiceReturns.Update(invoiceReturn);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceReturnDto>(invoiceReturn);
        }


        #region Helper Method

        private static string GenerateReturnNumber() => $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        private async Task IncreaseSalesRepInventoryForReturnAsync(int salesRepId, int productId, int quantity, int invoiceReturnId, string returnNumber)
        {
            if (quantity <= 0)
                throw new InvalidOperationException("Return quantity must be greater than zero.");

            var inventory = await _unitOfWork.SalesRepInventories
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.SalesRepId == salesRepId &&
                    x.ProductId == productId &&
                    x.IsActive);

            if (inventory == null)
            {
                inventory = new SalesRepInventory
                {
                    SalesRepId = salesRepId,
                    ProductId = productId,
                    Quantity = quantity,
                    LastUpdatedAt = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.SalesRepInventories.AddAsync(inventory);
            }
            else
            {
                inventory.Quantity += quantity;
                inventory.LastUpdatedAt = DateTime.UtcNow;
                inventory.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.SalesRepInventories.Update(inventory);
            }

            var movement = new SalesRepInventoryMovement
            {
                SalesRepId = salesRepId,
                ProductId = productId,
                Quantity = quantity,
                MovementType = SalesRepInventoryMovementType.In,
                Source = SalesRepInventoryMovementSource.InvoiceReturn,
                SourceId = invoiceReturnId,
                SourceNumber = returnNumber,
                MovementDate = DateTime.UtcNow,
                Notes = "Product returned from customer to sales rep."
            };

            await _unitOfWork.SalesRepInventoryMovements.AddAsync(movement);
        }

        #endregion

    }
}
