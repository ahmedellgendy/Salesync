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

        public async Task<InvoiceReturnDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid return id.");

            var invoiceReturn = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (invoiceReturn == null)
                throw new KeyNotFoundException(
                    $"Return with id {id} not found.");

            return MapReturnDto(invoiceReturn);
        }

        public async Task<IEnumerable<InvoiceReturnDto>> GetBySalesRepIdAsync(int salesRepId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            var returns = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .Where(x =>
                    x.SalesRepId == salesRepId &&
                    x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return returns
                .Select(MapReturnDto)
                .ToList();
        }

        public async Task<InvoiceReturnDto> CreateAsync(CreateInvoiceReturnDto dto)
        {
            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException(
                    "Return must contain at least one item.");

            var duplicateInvoiceItemIds = dto.Items
                .GroupBy(x => x.InvoiceItemId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateInvoiceItemIds.Count > 0)
                throw new InvalidOperationException(
                    "The same invoice item cannot be added more than once.");

            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .Include(i => i.InvoiceItems)
                .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId);

            if (invoice == null)
                throw new KeyNotFoundException(
                    $"Invoice with id {dto.InvoiceId} not found.");

            if (invoice.Status != InvoiceStatus.Confirmed)
                throw new InvalidOperationException(
                    "Returns are allowed only for confirmed invoices.");

            if (!invoice.InvoiceItems.Any())
                throw new InvalidOperationException(
                    "Cannot create return for invoice without items.");

            var previousReturns = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Where(x =>
                    x.InvoiceId == dto.InvoiceId &&
                    x.IsActive &&
                    x.Status != ReturnStatus.Rejected &&
                    x.Status != ReturnStatus.Cancelled)
                .ToListAsync();

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
                    .FirstOrDefault(i =>
                        i.Id == itemDto.InvoiceItemId);

                if (invoiceItem == null)
                    throw new InvalidOperationException(
                        $"Invoice item with id {itemDto.InvoiceItemId} does not belong to this invoice.");

                if (itemDto.Quantity <= 0)
                    throw new InvalidOperationException(
                        $"Return quantity for product {invoiceItem.ProductName} must be greater than zero.");

                var previouslyReturnedQuantity = previousReturns
                    .SelectMany(x => x.Items)
                    .Where(x =>
                        x.InvoiceItemId == invoiceItem.Id)
                    .Sum(x => x.Quantity);

                var remainingReturnableQuantity =
                    invoiceItem.Quantity -
                    previouslyReturnedQuantity;

                if (remainingReturnableQuantity <= 0)
                    throw new InvalidOperationException(
                        $"Product {invoiceItem.ProductName} has already been fully returned.");

                if (itemDto.Quantity > remainingReturnableQuantity)
                    throw new InvalidOperationException(
                        $"Return quantity for product {invoiceItem.ProductName} cannot exceed remaining returnable quantity ({remainingReturnableQuantity}).");

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

            invoiceReturn.TotalAmount =
                invoiceReturn.Items.Sum(i => i.TotalAmount);

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

        public async Task<InvoiceReturnDto> CancelAsync(int id)
        {
            var invoiceReturn = await _unitOfWork.InvoiceReturns
                .GetByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"Return with id {id} not found.");

            if (!invoiceReturn.IsActive)
                throw new InvalidOperationException(
                    "Cannot cancel inactive return.");

            if (invoiceReturn.Status != ReturnStatus.Pending)
                throw new InvalidOperationException(
                    "Only pending returns can be cancelled.");

            invoiceReturn.Status = ReturnStatus.Cancelled;
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

        private InvoiceReturnDto MapReturnDto(InvoiceReturn invoiceReturn)
        {
            return new InvoiceReturnDto
            {
                Id = invoiceReturn.Id,

                ReturnNumber = invoiceReturn.ReturnNumber,

                InvoiceId = invoiceReturn.InvoiceId,
                InvoiceNumber = invoiceReturn.Invoice?.InvoiceNumber,

                CustomerId = invoiceReturn.CustomerId,
                CustomerName = invoiceReturn.Customer?.Name,

                SalesRepId = invoiceReturn.SalesRepId,
                SalesRepSessionId = invoiceReturn.SalesRepSessionId,

                Status = invoiceReturn.Status,
                ReturnReason = invoiceReturn.ReturnReason,

                TotalAmount = invoiceReturn.TotalAmount,

                ReasonNotes = invoiceReturn.ReasonNotes,

                CreatedAt = invoiceReturn.CreatedAt,

                Items = invoiceReturn.Items
                    .Where(x => x.IsActive)
                    .Select(x => new InvoiceReturnItemDto
                    {
                        Id = x.Id,

                        InvoiceItemId = x.InvoiceItemId,

                        ProductId = x.ProductId,

                        ProductName = x.ProductName,
                        ItemCode = x.ItemCode,

                        Quantity = x.Quantity,

                        UnitPrice = x.UnitPrice,

                        TotalAmount = x.TotalAmount,

                        Notes = x.Notes
                    })
                    .ToList()
            };
        }

        #endregion

    }
}
