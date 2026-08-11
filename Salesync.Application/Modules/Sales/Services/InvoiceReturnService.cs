using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services
{
    public class InvoiceReturnService : IInvoiceReturnService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public InvoiceReturnService(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<InvoiceReturnDto>> GetByInvoiceIdAsync(
            int invoiceId)
        {
            if (invoiceId <= 0)
                throw new ArgumentException("Invalid invoice id.");

            var returns = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Invoice)
                .Include(x => x.Customer)
                .Where(x =>
                    x.InvoiceId == invoiceId &&
                    x.IsActive)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return returns
                .Select(MapReturnDto)
                .ToList();
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
            {
                throw new KeyNotFoundException(
                    $"Return with id {id} not found.");
            }

            return MapReturnDto(invoiceReturn);
        }

        public async Task<IEnumerable<InvoiceReturnDto>> GetBySalesRepIdAsync(
            int salesRepId)
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

        public async Task<InvoiceReturnDto> CreateAsync(
            CreateInvoiceReturnDto dto)
        {
            if (dto.Items == null ||
                dto.Items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Return must contain at least one item.");
            }

            if (!Enum.IsDefined(
                    typeof(ReturnReason),
                    dto.ReturnReason))
            {
                throw new InvalidOperationException(
                    "Invalid return reason.");
            }

            var duplicateInvoiceItemIds = dto.Items
                .GroupBy(x => x.InvoiceItemId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateInvoiceItemIds.Count > 0)
            {
                throw new InvalidOperationException(
                    "The same invoice item cannot be added more than once.");
            }

            var invoice = await _unitOfWork.Invoices
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.InvoiceItems)
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.InvoiceId &&
                    x.IsActive);

            if (invoice == null)
            {
                throw new KeyNotFoundException(
                    $"Invoice with id {dto.InvoiceId} not found.");
            }

            if (invoice.Status != InvoiceStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    "Returns are allowed only for confirmed invoices.");
            }

            var activeInvoiceItems = invoice.InvoiceItems
                .Where(x => x.IsActive)
                .ToList();

            if (activeInvoiceItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot create return for invoice without items.");
            }

            if (!dto.SalesRepSessionId.HasValue ||
                dto.SalesRepSessionId.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Sales rep session is required for invoice return.");
            }

            var salesRepId = invoice.SalesRepId
                ?? throw new InvalidOperationException(
                    "Invoice is not assigned to a sales rep.");

            var returnSession = await _unitOfWork.SalesRepSessions
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == dto.SalesRepSessionId.Value &&
                    x.SalesRepId == salesRepId &&
                    x.IsActive);

            if (returnSession == null)
            {
                throw new InvalidOperationException(
                    "Return session does not belong to invoice sales rep.");
            }

            if (returnSession.EndTime.HasValue)
            {
                throw new InvalidOperationException(
                    "Cannot create return for a closed session.");
            }

            if (returnSession.IsStockSettled)
            {
                throw new InvalidOperationException(
                    "Cannot create return after stock has been settled.");
            }

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

            /*
             * InvoiceItem.NetAmount already includes item-level discount.
             *
             * Invoice.DiscountAmount contains:
             * item discounts + invoice-level discount.
             *
             * So we extract the extra invoice-level discount and allocate
             * it proportionally across items.
             *
             * Tax is also allocated proportionally.
             */

            var invoiceItemsNetTotal =
                activeInvoiceItems.Sum(x => x.NetAmount);

            var invoiceItemsDiscountTotal =
                activeInvoiceItems.Sum(x => x.DiscountAmount);

            var extraInvoiceDiscount =
                invoice.DiscountAmount -
                invoiceItemsDiscountTotal;

            if (extraInvoiceDiscount < 0)
                extraInvoiceDiscount = 0;

            var invoiceReturn =
                _mapper.Map<InvoiceReturn>(dto);

            invoiceReturn.ReturnNumber =
                GenerateReturnNumber();

            invoiceReturn.CustomerId =
                invoice.CustomerId;

            invoiceReturn.SalesRepId =
                salesRepId;

            invoiceReturn.SalesRepSessionId =
                returnSession.Id;

            invoiceReturn.Status =
                ReturnStatus.Pending;

            invoiceReturn.CreatedAt =
                DateTime.UtcNow;

            invoiceReturn.IsActive =
                true;

            invoiceReturn.Items.Clear();

            foreach (var itemDto in dto.Items)
            {
                if (!Enum.IsDefined(
                        typeof(ReturnCondition),
                        itemDto.Condition))
                {
                    throw new InvalidOperationException(
                        "Invalid return item condition.");
                }

                if (itemDto.Quantity < 0)
                {
                    throw new InvalidOperationException(
                        "Return sale quantity cannot be negative.");
                }

                if (itemDto.BonusQuantity < 0)
                {
                    throw new InvalidOperationException(
                        "Return bonus quantity cannot be negative.");
                }

                if (itemDto.Quantity == 0 &&
                    itemDto.BonusQuantity == 0)
                {
                    throw new InvalidOperationException(
                        "At least one return quantity must be greater than zero.");
                }

                var invoiceItem = activeInvoiceItems
                    .FirstOrDefault(x =>
                        x.Id == itemDto.InvoiceItemId);

                if (invoiceItem == null)
                {
                    throw new InvalidOperationException(
                        $"Invoice item with id {itemDto.InvoiceItemId} does not belong to this invoice.");
                }

                /*
                 * Calculate previous PAID returns.
                 */

                var previouslyReturnedQuantity =
                    previousReturns
                        .SelectMany(x => x.Items)
                        .Where(x =>
                            x.IsActive &&
                            x.InvoiceItemId == invoiceItem.Id)
                        .Sum(x => x.Quantity);

                /*
                 * Calculate previous BONUS returns.
                 */

                var previouslyReturnedBonusQuantity =
                    previousReturns
                        .SelectMany(x => x.Items)
                        .Where(x =>
                            x.IsActive &&
                            x.InvoiceItemId == invoiceItem.Id)
                        .Sum(x => x.BonusQuantity);

                var remainingReturnableQuantity =
                    invoiceItem.Quantity -
                    previouslyReturnedQuantity;

                var remainingReturnableBonusQuantity =
                    invoiceItem.BonusQuantity -
                    previouslyReturnedBonusQuantity;

                if (itemDto.Quantity >
                    remainingReturnableQuantity)
                {
                    throw new InvalidOperationException(
                        $"Return sale quantity for product {invoiceItem.ProductName} cannot exceed remaining quantity ({remainingReturnableQuantity}).");
                }

                if (itemDto.BonusQuantity >
                    remainingReturnableBonusQuantity)
                {
                    throw new InvalidOperationException(
                        $"Return bonus quantity for product {invoiceItem.ProductName} cannot exceed remaining bonus quantity ({remainingReturnableBonusQuantity}).");
                }

                /*
                 * Financial calculation applies ONLY
                 * to paid/sale quantity.
                 *
                 * Bonus has zero financial return value.
                 */

                var returnAmount =
                    CalculateReturnAmount(
                        invoice,
                        invoiceItem,
                        invoiceItemsNetTotal,
                        extraInvoiceDiscount,
                        itemDto.Quantity);

                var returnItem = new InvoiceReturnItem
                {
                    InvoiceItemId =
                        invoiceItem.Id,

                    ProductId =
                        invoiceItem.ProductId,

                    ProductName =
                        invoiceItem.ProductName,

                    ItemCode =
                        invoiceItem.ItemCode,

                    Quantity =
                        itemDto.Quantity,

                    BonusQuantity =
                        itemDto.BonusQuantity,

                    /*
                     * Keep original selling unit price
                     * as an audit snapshot.
                     */
                    UnitPrice =
                        invoiceItem.UnitPrice,

                    /*
                     * Actual credit amount after
                     * discount/tax allocation.
                     */
                    TotalAmount =
                        returnAmount,

                    Condition =
                        itemDto.Condition,

                    Notes =
                        itemDto.Notes,

                    CreatedAt =
                        DateTime.UtcNow,

                    IsActive =
                        true
                };

                invoiceReturn.Items.Add(returnItem);
            }

            invoiceReturn.TotalAmount =
                invoiceReturn.Items
                    .Sum(x => x.TotalAmount);

            await _unitOfWork.InvoiceReturns
                .AddAsync(invoiceReturn);

            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(
                invoiceReturn.Id);
        }

        public async Task<InvoiceReturnDto> ApproveAsync(
            int id)
        {
            if (id <= 0)
                throw new ArgumentException(
                    "Invalid return id.");

            var invoiceReturn = await _unitOfWork.InvoiceReturns
                .GetQueryable()
                .Include(x => x.Items)
                .Include(x => x.Invoice)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (invoiceReturn == null)
            {
                throw new KeyNotFoundException(
                    $"Return with id {id} not found.");
            }

            if (invoiceReturn.Status !=
                ReturnStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending returns can be approved.");
            }

            var activeItems = invoiceReturn.Items
                .Where(x => x.IsActive)
                .ToList();

            if (activeItems.Count == 0)
            {
                throw new InvalidOperationException(
                    "Cannot approve return without items.");
            }

            /*
             * A bonus-only return can legitimately have
             * TotalAmount = 0.
             *
             * So TotalAmount > 0 is NOT required.
             */

            if (activeItems.Any(x =>
                    x.Quantity < 0 ||
                    x.BonusQuantity < 0 ||
                    (x.Quantity == 0 &&
                     x.BonusQuantity == 0)))
            {
                throw new InvalidOperationException(
                    "Return contains invalid quantities.");
            }

            if (invoiceReturn.Invoice == null)
            {
                throw new KeyNotFoundException(
                    $"Invoice with id {invoiceReturn.InvoiceId} not found.");
            }

            if (invoiceReturn.Invoice.Status !=
                InvoiceStatus.Confirmed)
            {
                throw new InvalidOperationException(
                    "Cannot approve return for unconfirmed invoice.");
            }

            var salesRepId =
                invoiceReturn.SalesRepId
                ?? invoiceReturn.Invoice.SalesRepId
                ?? throw new InvalidOperationException(
                    "Sales rep is required for invoice return.");

            if (!invoiceReturn.SalesRepSessionId.HasValue ||
                invoiceReturn.SalesRepSessionId.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Sales rep session is required for invoice return.");
            }

            var returnSessionExists =
                await _unitOfWork.SalesRepSessions
                    .GetQueryable()
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.Id ==
                            invoiceReturn.SalesRepSessionId.Value &&
                        x.SalesRepId ==
                            salesRepId &&
                        x.IsActive);

            if (!returnSessionExists)
            {
                throw new InvalidOperationException(
                    "Return session not found for sales rep.");
            }

            /*
             * Approval is administrative only.
             *
             * Returned stock must NOT be added to
             * SalesRepInventory here.
             *
             * Good / Damaged / Expired products will be
             * handled later during warehouse receiving.
             */

            invoiceReturn.Status =
                ReturnStatus.Approved;

            invoiceReturn.UpdatedAt =
                DateTime.UtcNow;

            _unitOfWork.InvoiceReturns
                .Update(invoiceReturn);

            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(id);
        }

        public async Task<InvoiceReturnDto> RejectAsync(
            int id)
        {
            if (id <= 0)
                throw new ArgumentException(
                    "Invalid return id.");

            var invoiceReturn =
                await _unitOfWork.InvoiceReturns
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.IsActive);

            if (invoiceReturn == null)
            {
                throw new KeyNotFoundException(
                    $"Return with id {id} not found.");
            }

            if (invoiceReturn.Status !=
                ReturnStatus.Pending)
            {
                throw new InvalidOperationException(
                    "Only pending returns can be rejected.");
            }

            invoiceReturn.Status =
                ReturnStatus.Rejected;

            invoiceReturn.UpdatedAt =
                DateTime.UtcNow;

            _unitOfWork.InvoiceReturns
                .Update(invoiceReturn);

            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(id);
        }

        public async Task<InvoiceReturnDto> CancelAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid return id.");

            var invoiceReturn =await _unitOfWork.InvoiceReturns
                    .GetQueryable()
                    .FirstOrDefaultAsync(x =>
                        x.Id == id &&
                        x.IsActive);

            if (invoiceReturn == null)
            {
                throw new KeyNotFoundException($"Return with id {id} not found.");
            }

            if (invoiceReturn.Status !=ReturnStatus.Pending)
            {
                throw new InvalidOperationException("Only pending returns can be cancelled.");
            }

            invoiceReturn.Status =ReturnStatus.Cancelled;

            invoiceReturn.UpdatedAt =DateTime.UtcNow;

            _unitOfWork.InvoiceReturns.Update(invoiceReturn);

            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(id);
        }

        #region Helper Methods

        private static string GenerateReturnNumber()
        {
            return
                $"RET-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        }

        private static decimal CalculateReturnAmount(Invoice invoice, InvoiceItem invoiceItem, decimal invoiceItemsNetTotal, decimal extraInvoiceDiscount, int returnQuantity)
        {
            if (returnQuantity <= 0)
                return 0m;

            if (invoiceItem.Quantity <= 0)
                return 0m;

            /*
             * InvoiceItem.NetAmount already contains
             * item-level discount.
             */

            var itemNetAmount = invoiceItem.NetAmount;

            /*
             * Allocate invoice-level discount
             * proportionally based on item net value.
             */

            var itemRatio = invoiceItemsNetTotal > 0
                    ? itemNetAmount / invoiceItemsNetTotal
                    : 0m;

            var itemExtraDiscountShare = extraInvoiceDiscount * itemRatio;

            /*
             * Allocate invoice tax using same ratio.
             */

            var itemTaxShare = invoice.TaxAmount * itemRatio;

            var itemFinalAmount = itemNetAmount - itemExtraDiscountShare + itemTaxShare;

            if (itemFinalAmount < 0)
                itemFinalAmount = 0;

            /*
             * Financial value per PAID small unit.
             * Bonus units are not included here.
             */

            var effectiveUnitAmount = itemFinalAmount / invoiceItem.Quantity;

            var returnAmount = effectiveUnitAmount * returnQuantity;

            return decimal.Round(returnAmount, 2, MidpointRounding.AwayFromZero);
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
                    .Select(x =>
                        new InvoiceReturnItemDto
                        {
                            Id = x.Id,
                            InvoiceItemId = x.InvoiceItemId,
                            ProductId = x.ProductId,
                            ProductName = x.ProductName,
                            ItemCode = x.ItemCode,
                            Quantity = x.Quantity,
                            BonusQuantity = x.BonusQuantity,
                            UnitPrice = x.UnitPrice,
                            TotalAmount = x.TotalAmount,
                            Condition = x.Condition,
                            Notes = x.Notes
                        })
                    .ToList()
            };
        }

        #endregion
    }
}