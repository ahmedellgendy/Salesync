using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateInvoiceDto> _createInvoiceValidator;
        private readonly ICurrentUserService _currentUser;
        private readonly IInventoryService _inventoryService;



        public InvoiceService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateInvoiceDto> createInvoiceValidator, ICurrentUserService currentUser, IInventoryService inventoryService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createInvoiceValidator = createInvoiceValidator;
            _currentUser = currentUser;
            _inventoryService = inventoryService;
        }
        public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
        {
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            return _mapper.Map<IEnumerable<InvoiceDto>>(invoices);
        }
        public async Task<InvoiceDto> GetByIdAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices
                 .GetQueryable()
                 .Include(i => i.InvoiceItems)
                 .Include(i => i.Payments)
                 .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                throw new KeyNotFoundException($"Invoice with id {id} not found.");

            return _mapper.Map<InvoiceDto>(invoice);
        }
        public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
        {
            var validationResult = await _createInvoiceValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId)
                ?? throw new KeyNotFoundException($"Customer with id {dto.CustomerId} not found.");

            if (!customer.IsActive)
                throw new InvalidOperationException("Cannot create invoice for inactive customer.");

            var warehouse = await _unitOfWork.Warehouses.GetByIdAsync(dto.WarehouseId)
                ?? throw new KeyNotFoundException($"Warehouse with id {dto.WarehouseId} not found.");

            if (!warehouse.IsActive)
                throw new InvalidOperationException("Cannot create invoice for inactive warehouse.");

            if (string.IsNullOrWhiteSpace(_currentUser.UserId))
                throw new UnauthorizedAccessException("Current user is not authenticated.");

            Salesync.Domain.Modules.SalesRep.Entities.SalesRep salesRep;

            var isSalesRepUser = string.Equals(_currentUser.Role, "SalesRep", StringComparison.OrdinalIgnoreCase);

            if (isSalesRepUser)
            {
                salesRep = (await _unitOfWork.SalesReps
                   .FindAsync(s => s.UserId == _currentUser.UserId && s.IsActive))
                   .FirstOrDefault()
                   ?? throw new UnauthorizedAccessException("SalesRep not found for current user.");
            }
            else
            {
                if (!dto.SalesRepId.HasValue)
                    throw new InvalidOperationException("SalesRepId is required for admin or supervisor invoice creation.");

                salesRep = await _unitOfWork.SalesReps.GetByIdAsync(dto.SalesRepId.Value)
                    ?? throw new KeyNotFoundException($"SalesRep with id {dto.SalesRepId.Value} not found.");

                if (!salesRep.IsActive)
                    throw new InvalidOperationException("Cannot create invoice for inactive sales rep.");
            }

            if (!dto.SalesRepSessionId.HasValue)
                throw new InvalidOperationException("SalesRepSessionId is required.");

            var session = await _unitOfWork.SalesRepSessions.GetByIdAsync(dto.SalesRepSessionId.Value)
                ?? throw new KeyNotFoundException($"SalesRepSession with id {dto.SalesRepSessionId.Value} not found.");

            if (!session.IsActive)
                throw new InvalidOperationException("Cannot create invoice for inactive session.");

            if (session.Status == DayStatus.Closed)
                throw new InvalidOperationException("Cannot create invoice for a closed session.");

            if (session.SalesRepId != salesRep.Id)
                throw new UnauthorizedAccessException("You cannot create invoice using another sales rep's session.");



            var invoice = _mapper.Map<Invoice>(dto);
            invoice.InvoiceNumber = GenerateInvoiceNumber();
            invoice.Status = InvoiceStatus.Draft;
            invoice.PaymentStatus = PaymentStatus.Pending;
            invoice.CreatedAt = DateTime.UtcNow;
            invoice.IsActive = true;
            invoice.SalesRepId = salesRep.Id;
            invoice.SalesRepSessionId = session.Id;

            invoice.InvoiceItems.Clear();

            foreach (var itemDto in dto.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId)
                    ?? throw new KeyNotFoundException($"Product with id {itemDto.ProductId} not found.");
                if (!product.IsActive)
                    throw new InvalidOperationException($"Product with id {itemDto.ProductId} is inactive.");

                var grossAmount = product.UnitPrice * itemDto.Quantity;
                var discountAmount = grossAmount * (itemDto.DiscountPercentage / 100m);
                var netAmount = grossAmount - discountAmount;

                var invoiceItem = new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    ProductName = product.Name,
                    ItemCode = product.ItemCode,
                    Quantity = itemDto.Quantity,
                    BonusQuantity = itemDto.BonusQuantity,
                    UnitPrice = product.UnitPrice,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    DiscountAmount = discountAmount,
                    NetAmount = netAmount,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                invoice.InvoiceItems.Add(invoiceItem);
            }

            invoice.SubTotal = invoice.InvoiceItems.Sum(i => i.NetAmount);

            if (invoice.DiscountAmount > invoice.SubTotal)
                throw new InvalidOperationException("Invoice discount cannot be greater than invoice subtotal.");

            invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<InvoiceDto>(invoice);
        }
        public async Task<InvoiceDto> UpdateAsync(int id, UpdateInvoiceDto dto)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with id {id} not found.");

            _mapper.Map(dto, invoice);
            invoice.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<InvoiceDto>(invoice);
        }

        public async Task<InvoiceDto> ConfirmAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices
                     .GetQueryable()
                     .Include(i => i.InvoiceItems)
                     .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                throw new KeyNotFoundException($"Invoice with id {id} not found.");

            if (!invoice.IsActive)
                throw new InvalidOperationException("Cannot confirm inactive invoice.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException("Cannot confirm a cancelled invoice.");

            if (invoice.Status == InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Invoice is already confirmed.");

            if (!invoice.InvoiceItems.Any())
                throw new InvalidOperationException("Cannot confirm invoice without items.");

            if (invoice.TotalAmount <= 0)
                throw new InvalidOperationException("Cannot confirm invoice with invalid total amount.");

            if (invoice.WarehouseId <= 0)
                throw new InvalidOperationException("Invoice warehouse is required.");

            var requiredStock = invoice.InvoiceItems
                 .GroupBy(i => i.ProductId)
                 .Select(g => new
                 {
                     ProductId = g.Key,
                     ProductName = g.First().ProductName,
                     Quantity = g.Sum(i => i.Quantity + i.BonusQuantity)
                 })
                 .ToList();

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                foreach (var item in requiredStock)
                {
                    if (item.Quantity <= 0)
                        throw new InvalidOperationException($"Invalid stock quantity for product {item.ProductName}.");

                    var balance = await _unitOfWork.StockBalances
                        .GetQueryable()
                        .FirstOrDefaultAsync(x =>
                            x.ProductId == item.ProductId &&
                            x.WarehouseId == invoice.WarehouseId &&
                            x.IsActive);

                    if (balance == null)
                        throw new InvalidOperationException($"No stock balance found for product {item.ProductName}.");

                    if (balance.Quantity < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for product {item.ProductName}. Available: {balance.Quantity}, Required: {item.Quantity}.");
                }

                foreach (var item in requiredStock)
                {
                    await _inventoryService.StockOutAsync(
                        item.ProductId,
                        invoice.WarehouseId,
                        item.Quantity,
                        StockMovementSource.Invoice,
                        invoice.Id,
                        invoice.InvoiceNumber,
                        $"Stock out for invoice {invoice.InvoiceNumber}");
                }

                invoice.Status = InvoiceStatus.Confirmed;
                invoice.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Invoices.Update(invoice);
                await _unitOfWork.CompleteAsync();

                await _unitOfWork.CommitTransactionAsync();

                return _mapper.Map<InvoiceDto>(invoice);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task CancelAsync(int id)
        {
            var invoice = await _unitOfWork.Invoices.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Invoice with id {id} not found.");

            if (invoice.Status == InvoiceStatus.Confirmed)
                throw new InvalidOperationException("Cannot cancel a confirmed invoice.");

            invoice.Status = InvoiceStatus.Cancelled;
            invoice.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.CompleteAsync();
        }

        #region Helper Method

        private static string GenerateInvoiceNumber() => $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        #endregion

    }
}
