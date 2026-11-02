using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.MasterData;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;

namespace Salesync.Application.Modules.Sales.Services;

public sealed class InvoiceReturnReceivingService(IUnitOfWork unit, IInventoryService inventory,
    ISalesAccessScopeService access, ICurrentUserService user) : IInvoiceReturnReceivingService
{
    private async Task<InvoiceReturn> OwnReturnAsync(int id)
    {
        var record = await unit.InvoiceReturns.GetQueryable().Include(x => x.Items)
            .Include(x => x.Invoice).Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == id && x.IsActive)
            ?? throw new KeyNotFoundException("Return not found.");
        await access.RequireSalesRepAsync(record.SalesRepId);
        return record;
    }

    public async Task<ReturnReceivingDto> GetAsync(int returnId)
    {
        var record = await OwnReturnAsync(returnId);
        var receipts = await unit.InvoiceReturnReceipts.GetQueryable().AsNoTracking()
            .Where(x => x.InvoiceReturnId == returnId).Include(x => x.Items)
            .OrderBy(x => x.Id).ToListAsync();
        var scope = await access.GetAsync();
        var credit = await unit.InvoiceReturnReceipts.GetQueryable().AsNoTracking()
            .Where(x => x.CustomerId == record.CustomerId &&
                (scope.AllowsAll || scope.SalesRepIds.Contains(x.SalesRepId ?? 0)))
            .SumAsync(x => (decimal?)x.CustomerCreditAmount) ?? 0m;
        var lines = receipts.SelectMany(x => x.Items).ToList();
        return new() {
            ReturnId = record.Id, ReturnNumber = record.ReturnNumber, CustomerId = record.CustomerId,
            CustomerName = record.Customer.Name, InvoiceNumber = record.Invoice.InvoiceNumber, Status = record.Status,
            InvoiceRemainingAmount = Math.Max(0m, record.Invoice.TotalAmount - record.Invoice.PaidAmount - record.Invoice.CreditAppliedAmount),
            TotalReceivedCreditAmount = receipts.Sum(x => x.TotalAmount),
            TotalAppliedToInvoiceAmount = receipts.Sum(x => x.AppliedToInvoiceAmount), CustomerCreditBalance = credit,
            Items = record.Items.Where(x => x.IsActive).Select(x => new ReturnReceivingLineDto {
                Id = x.Id, ProductName = x.ProductName, Condition = x.Condition, Quantity = x.Quantity, BonusQuantity = x.BonusQuantity,
                ReceivedQuantity = lines.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.Quantity),
                ReceivedBonusQuantity = lines.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.BonusQuantity),
                ReceivedCreditAmount = lines.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.TotalAmount)
            }).ToList(), Receipts = receipts.Select(Map).ToList()
        };
    }

    public async Task<ReturnReceiptDto?> FindOperationAsync(int returnId, Guid operationId)
    {
        await OwnReturnAsync(returnId);
        var receipt = await unit.InvoiceReturnReceipts.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(x => x.InvoiceReturnId == returnId && x.OperationId == operationId);
        return receipt is null ? null : Map(receipt);
    }

    public Task<ReturnReceiptDto> ReceiveAsync(int returnId, ReceiveInvoiceReturnDto dto) =>
        unit.ExecuteInTransactionAsync(() => ReceiveCoreAsync(returnId, dto));

    private async Task<ReturnReceiptDto> ReceiveCoreAsync(int returnId, ReceiveInvoiceReturnDto dto)
    {
        if (user.Role is not ("Admin" or "Warehouse"))
            throw new UnauthorizedAccessException("Only administration or warehouse can receive returns.");
        var record = await OwnReturnAsync(returnId);
        if (dto.OperationId == Guid.Empty || dto.Items is null || (dto.Notes?.Length ?? 0) > 500 ||
            dto.Items.GroupBy(x => x.InvoiceReturnItemId).Any(g => g.Count() != 1))
            throw new ArgumentException("Invalid receiving command or duplicate lines.");
        var normalized = new { returnId, dto.CloseRemaining, Notes = dto.Notes?.Trim(),
            Items = dto.Items.OrderBy(x => x.InvoiceReturnItemId).Select(x => new {
                x.InvoiceReturnItemId, x.WarehouseId, x.Quantity, x.BonusQuantity }).ToArray() };
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized))));
        var existing = await unit.InvoiceReturnReceipts.GetQueryable().FirstOrDefaultAsync(x => x.OperationId == dto.OperationId);
        if (existing is not null) {
            if (existing.InvoiceReturnId != returnId || existing.RequestHash != hash)
                throw new InvalidOperationException("OPERATION_CONFLICT: Operation key was used for different receiving data.");
            return Map(existing);
        }
        if (record.Status is not (ReturnStatus.Approved or ReturnStatus.PartiallyReceived) ||
            record.Invoice.Status != InvoiceStatus.Confirmed || !record.Invoice.IsActive)
            throw new InvalidOperationException("Only approved, unfinished returns of confirmed invoices can be received.");

        var prior = await unit.InvoiceReturnReceiptItems.GetQueryable().AsNoTracking()
            .Where(x => x.Receipt.InvoiceReturnId == returnId).ToListAsync();
        var originals = record.Items.Where(x => x.IsActive).ToDictionary(x => x.Id);
        var lines = new List<InvoiceReturnReceiptItem>();
        foreach (var item in dto.Items) {
            if (!originals.TryGetValue(item.InvoiceReturnItemId, out var original) || item.Quantity < 0 || item.BonusQuantity < 0 ||
                (item.Quantity == 0 && item.BonusQuantity == 0))
                throw new ArgumentException("Invalid receiving line or item outside the return.");
            var paidBefore = prior.Where(x => x.InvoiceReturnItemId == original.Id).Sum(x => x.Quantity);
            var bonusBefore = prior.Where(x => x.InvoiceReturnItemId == original.Id).Sum(x => x.BonusQuantity);
            if (item.Quantity > original.Quantity - paidBefore || item.BonusQuantity > original.BonusQuantity - bonusBefore)
                throw new InvalidOperationException("Receiving exceeds the remaining authorized quantity.");
            await access.RequireWarehouseAsync(item.WarehouseId);
            var warehouse = await unit.Warehouses.GetByIdAsync(item.WarehouseId)
                ?? throw new KeyNotFoundException("Warehouse not found.");
            if (!warehouse.IsActive || (original.Condition != ReturnCondition.Good && warehouse.Type != WarehouseType.Damaged) ||
                (original.Condition == ReturnCondition.Good && warehouse.Type == WarehouseType.Damaged))
                throw new InvalidOperationException("Damaged/expired returns require a damaged warehouse; good stock requires a normal warehouse.");
            var cumulativeValue = original.Quantity == 0 ? 0m :
                Math.Round(original.TotalAmount * (paidBefore + item.Quantity) / original.Quantity, 2, MidpointRounding.AwayFromZero);
            var lineValue = cumulativeValue - prior.Where(x => x.InvoiceReturnItemId == original.Id).Sum(x => x.TotalAmount);
            if (lineValue < 0m) throw new InvalidOperationException("Inconsistent prior return credit.");
            _ = checked(item.Quantity + item.BonusQuantity);
            lines.Add(new() { InvoiceReturnItemId = original.Id, ProductId = original.ProductId,
                ProductName = original.ProductName, ItemCode = original.ItemCode, Quantity = item.Quantity,
                BonusQuantity = item.BonusQuantity, WarehouseId = item.WarehouseId, Condition = original.Condition, TotalAmount = lineValue });
        }
        if (lines.Count == 0 && !(dto.CloseRemaining && prior.Count > 0))
            throw new ArgumentException("Receive at least one unit, or explicitly finish a previously partial receipt.");
        var remains = originals.Values.Any(x =>
            prior.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.Quantity) + lines.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.Quantity) < x.Quantity ||
            prior.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.BonusQuantity) + lines.Where(y => y.InvoiceReturnItemId == x.Id).Sum(y => y.BonusQuantity) < x.BonusQuantity);
        if (remains && dto.CloseRemaining && string.IsNullOrWhiteSpace(dto.Notes))
            throw new ArgumentException("A reason is required to finish unreceived quantities.");
        var amount = lines.Sum(x => x.TotalAmount);
        var outstanding = Math.Max(0m, record.Invoice.TotalAmount - record.Invoice.PaidAmount - record.Invoice.CreditAppliedAmount);
        var applied = Math.Min(amount, outstanding);
        var sessionId = await unit.SalesRepSessions.GetQueryable().Where(x => x.SalesRepId == record.SalesRepId && x.IsActive &&
            x.Status != DayStatus.Closed && !x.EndTime.HasValue && !x.IsTreasurySettled)
            .OrderByDescending(x => x.StartTime).Select(x => (int?)x.Id).FirstOrDefaultAsync();
        var receipt = new InvoiceReturnReceipt {
            OperationId = dto.OperationId, RequestHash = hash,
            ReceiptNumber = $"RR-{dto.OperationId:N}", CreditNoteNumber = $"CN-{dto.OperationId:N}",
            InvoiceReturnId = returnId, InvoiceId = record.InvoiceId, CustomerId = record.CustomerId,
            SalesRepId = record.SalesRepId, SalesRepSessionId = sessionId, TotalAmount = amount,
            AppliedToInvoiceAmount = applied, CustomerCreditAmount = amount - applied,
            CloseRemaining = dto.CloseRemaining, Notes = dto.Notes?.Trim(), CreatedBy = user.UserId,
            Items = lines
        };
        await unit.InvoiceReturnReceipts.AddAsync(receipt);
        await unit.CompleteAsync();
        foreach (var line in lines)
            await inventory.StockInAsync(line.ProductId, line.WarehouseId, checked(line.Quantity + line.BonusQuantity),
                StockMovementSource.InvoiceReturn, receipt.Id, receipt.ReceiptNumber, $"Return {record.ReturnNumber}; condition {line.Condition}");
        record.Invoice.CreditAppliedAmount += applied;
        record.Invoice.PaymentStatus = record.Invoice.PaidAmount + record.Invoice.CreditAppliedAmount >= record.Invoice.TotalAmount
            ? PaymentStatus.Paid : record.Invoice.PaidAmount + record.Invoice.CreditAppliedAmount > 0 ? PaymentStatus.PartiallyPaid : PaymentStatus.Pending;
        record.Invoice.UpdatedAt = DateTime.UtcNow;
        record.Status = !remains || dto.CloseRemaining ? ReturnStatus.ReceivedAtWarehouse : ReturnStatus.PartiallyReceived;
        record.UpdatedAt = DateTime.UtcNow;
        unit.Invoices.Update(record.Invoice);
        unit.InvoiceReturns.Update(record);
        await unit.CompleteAsync();
        return Map(receipt);
    }

    private static ReturnReceiptDto Map(InvoiceReturnReceipt x) => new() {
        Id = x.Id, OperationId = x.OperationId, ReceiptNumber = x.ReceiptNumber, CreditNoteNumber = x.CreditNoteNumber,
        PostedAt = x.CreatedAt, PostingSessionId = x.SalesRepSessionId, TotalAmount = x.TotalAmount,
        AppliedToInvoiceAmount = x.AppliedToInvoiceAmount, CustomerCreditAmount = x.CustomerCreditAmount,
        CloseRemaining = x.CloseRemaining, Notes = x.Notes
    };
}
