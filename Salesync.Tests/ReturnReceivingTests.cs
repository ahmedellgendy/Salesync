using Microsoft.EntityFrameworkCore;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Application.Common.Exceptions;
using Salesync.Domain.Common.Enums.Inventory;
using Salesync.Domain.Common.Enums.MasterData;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Xunit;

namespace Salesync.Tests;

public class ReturnReceivingTests
{
    private static async Task SeedAsync(WorkflowFixture f, decimal paid = 700m,
        decimal value = 500m, int quantity = 10, int bonus = 0, ReturnCondition condition = ReturnCondition.Good)
    {
        await f.SessionAsync();
        f.Actor.Role = "Admin";
        f.Db.Add(new Product { Id = 1, Name = "Product", ItemCode = "P1", UnitsPerLargeUnit = 24, UnitPrice = 50 });
        f.Db.Add(new Invoice { Id = 1, InvoiceNumber = "INV1", CustomerId = 1, SalesRepId = 1,
            WarehouseId = 1, SalesRepSessionId = 1, TotalAmount = 1000, PaidAmount = paid, Status = InvoiceStatus.Confirmed });
        f.Db.Add(new InvoiceItem { Id = 1, InvoiceId = 1, ProductId = 1, ProductName = "Product", ItemCode = "P1",
            Quantity = 20, BonusQuantity = bonus, UnitPrice = 50, NetAmount = 1000 });
        f.Db.Add(new InvoiceReturn { Id = 1, ReturnNumber = "RET1", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Status = ReturnStatus.Approved, TotalAmount = value });
        f.Db.Add(new InvoiceReturnItem { Id = 1, InvoiceReturnId = 1, InvoiceItemId = 1, ProductId = 1,
            ProductName = "Product", ItemCode = "P1", Quantity = quantity, BonusQuantity = bonus,
            UnitPrice = 50, TotalAmount = value, Condition = condition });
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
    }

    private static ReceiveInvoiceReturnDto Command(int qty = 10, int bonus = 0, int warehouse = 1) => new() {
        OperationId = Guid.NewGuid(), Items = [new() { InvoiceReturnItemId = 1, WarehouseId = warehouse, Quantity = qty, BonusQuantity = bonus }]
    };

    [Theory]
    [InlineData(700, 200, 200, 0, 100)]
    [InlineData(700, 500, 300, 200, 0)]
    [InlineData(1000, 500, 0, 500, 0)]
    public async Task Receipt_offsets_debt_then_retains_surplus_without_changing_original_cash_or_total(
        int paid, int value, int applied, int surplus, int remaining)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await SeedAsync(f, paid, value);
        var service = f.Get<IInvoiceReturnReceivingService>();
        var result = await service.ReceiveAsync(1, Command());
        Assert.Equal(value, result.TotalAmount);
        Assert.Equal(applied, result.AppliedToInvoiceAmount);
        Assert.Equal(surplus, result.CustomerCreditAmount);
        var invoice = await f.Db.Invoices.AsNoTracking().SingleAsync();
        Assert.Equal(1000, invoice.TotalAmount);
        Assert.Equal(paid, invoice.PaidAmount);
        Assert.Equal(applied, invoice.CreditAppliedAmount);
        var detail = await service.GetAsync(1);
        Assert.Equal(remaining, detail.InvoiceRemainingAmount);
        Assert.Equal(surplus, detail.CustomerCreditBalance);
        Assert.Equal(10, (await f.Db.StockBalances.SingleAsync()).Quantity);
        Assert.Single(await f.Db.StockMovements.ToListAsync());
        Assert.Empty(await f.Db.Payments.ToListAsync());
        Assert.Empty(await f.Db.CashReceipts.ToListAsync());
        Assert.Equal(ReturnStatus.ReceivedAtWarehouse, detail.Status);
    }

    [Fact]
    public async Task Same_operation_replays_once_and_changed_payload_conflicts()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        var service = f.Get<IInvoiceReturnReceivingService>(); var command = Command();
        var first = await service.ReceiveAsync(1, command);
        Assert.Equal(first.Id, (await service.ReceiveAsync(1, command)).Id);
        Assert.Equal(first.Id, (await service.FindOperationAsync(1, command.OperationId))!.Id);
        command.Items[0].Quantity = 9;
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAsync(1, command));
        Assert.Contains("OPERATION_CONFLICT", error.Message);
        Assert.Single(await f.Db.InvoiceReturnReceipts.ToListAsync());
        Assert.Single(await f.Db.StockMovements.ToListAsync());
        Assert.Equal(300, (await f.Db.Invoices.AsNoTracking().SingleAsync()).CreditAppliedAmount);
    }

    [Fact]
    public async Task Partial_receipts_preserve_rounding_and_reject_excess()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f, value: 1, quantity: 3);
        var service = f.Get<IInvoiceReturnReceivingService>();
        Assert.Equal(.33m, (await service.ReceiveAsync(1, Command(1))).TotalAmount);
        Assert.Equal(ReturnStatus.PartiallyReceived, (await service.GetAsync(1)).Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAsync(1, Command(3)));
        f.Db.ChangeTracker.Clear();
        Assert.Equal(.34m, (await service.ReceiveAsync(1, Command(1))).TotalAmount);
        Assert.Equal(.33m, (await service.ReceiveAsync(1, Command(1))).TotalAmount);
        Assert.Equal(1m, (await service.GetAsync(1)).TotalReceivedCreditAmount);
        Assert.Equal(3, (await f.Db.StockBalances.AsNoTracking().SingleAsync()).Quantity);
    }

    [Fact]
    public async Task Bonus_receipt_has_stock_but_no_financial_credit()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f, value: 0, quantity: 0, bonus: 2);
        var result = await f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command(0, 2));
        Assert.Equal(0, result.TotalAmount);
        Assert.Equal(0, (await f.Db.Invoices.AsNoTracking().SingleAsync()).CreditAppliedAmount);
        Assert.Equal(2, (await f.Db.StockBalances.SingleAsync()).Quantity);
    }

    [Theory]
    [InlineData(ReturnCondition.Damaged)]
    [InlineData(ReturnCondition.Expired)]
    public async Task Unsaleable_receipts_require_quarantine_and_cannot_be_loaded(ReturnCondition condition)
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f, condition: condition);
        var service = f.Get<IInvoiceReturnReceivingService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAsync(1, Command()));
        f.Db.ChangeTracker.Clear();
        var warehouse = await f.Db.Warehouses.SingleAsync(x => x.Id == 1); warehouse.Type = WarehouseType.Damaged;
        await f.Db.SaveChangesAsync();
        await service.ReceiveAsync(1, Command());
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Get<IInventoryService>().StockOutAsync(1, 1, 1,
            StockMovementSource.LoadRequest, 99, "LOAD99", null));
        Assert.Equal(10, (await f.Db.StockBalances.AsNoTracking().SingleAsync()).Quantity);
    }

    [Fact]
    public async Task Failed_stock_posting_rolls_back_document_and_credit_and_can_retry_same_key()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        var service = f.Get<IInvoiceReturnReceivingService>(); var command = Command(); f.Db.FailReturnStock = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAsync(1, command));
        f.Db.ChangeTracker.Clear(); f.Db.FailReturnStock = false;
        Assert.Empty(await f.Db.InvoiceReturnReceipts.ToListAsync());
        Assert.Empty(await f.Db.StockBalances.ToListAsync());
        Assert.Equal(0, (await f.Db.Invoices.AsNoTracking().SingleAsync()).CreditAppliedAmount);
        await service.ReceiveAsync(1, command);
        Assert.Single(await f.Db.InvoiceReturnReceipts.ToListAsync());
    }

    [Fact]
    public async Task Late_receipt_posts_to_active_day_and_does_not_reopen_original_day()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        var old = await f.Db.SalesRepSessions.SingleAsync(); old.Status = DayStatus.Closed; old.EndTime = DateTime.UtcNow.AddHours(-2);
        await f.Db.SaveChangesAsync(); await f.SessionAsync(2);
        var result = await f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command());
        Assert.Equal(2, result.PostingSessionId);
        Assert.Equal(DayStatus.Closed, (await f.Db.SalesRepSessions.AsNoTracking().SingleAsync(x => x.Id == 1)).Status);
    }

    [Fact]
    public async Task Finalizing_short_receipt_requires_reason_and_releases_unreceived_quantity()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        var service = f.Get<IInvoiceReturnReceivingService>(); await service.ReceiveAsync(1, Command(2));
        var finish = new ReceiveInvoiceReturnDto { OperationId = Guid.NewGuid(), CloseRemaining = true };
        await Assert.ThrowsAsync<ArgumentException>(() => service.ReceiveAsync(1, finish)); f.Db.ChangeTracker.Clear();
        finish.Notes = "Remaining units not delivered"; await service.ReceiveAsync(1, finish);
        var available = await f.Get<IInvoiceReturnService>().GetReturnableInvoiceDetailsAsync(1, 1);
        Assert.Equal(18, Assert.Single(available.Items).RemainingReturnableQuantity);
        Assert.Equal(100m, (await service.GetAsync(1)).TotalReceivedCreditAmount);
    }

    [Fact]
    public async Task SalesRep_cannot_post_warehouse_receipt()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f); f.Actor.Role = "SalesRep";
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command()));
        Assert.Empty(await f.Db.InvoiceReturnReceipts.ToListAsync());
    }

    [Theory]
    [InlineData(ReturnStatus.Pending)]
    [InlineData(ReturnStatus.Rejected)]
    [InlineData(ReturnStatus.Cancelled)]
    [InlineData(ReturnStatus.ReceivedAtWarehouse)]
    public async Task Only_approved_unfinished_requests_can_be_received(ReturnStatus status)
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        (await f.Db.InvoiceReturns.SingleAsync()).Status = status; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command()));
        Assert.Empty(await f.Db.StockMovements.ToListAsync());
        Assert.Empty(await f.Db.InvoiceReturnReceipts.ToListAsync());
    }

    [Fact]
    public async Task Warehouse_cannot_receive_outside_its_branch()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f); f.Actor.Role = "Warehouse";
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command(warehouse: 2)));
        Assert.Empty(await f.Db.InvoiceReturnReceipts.ToListAsync());
        (await f.Db.InvoiceReturns.SingleAsync()).SalesRepId = 2; await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenException>(() => f.Get<IInvoiceReturnReceivingService>().GetAsync(1));
    }

    [Fact]
    public async Task Credit_reduces_authoritative_remaining_and_payment_limit_but_is_never_a_collection()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f, value: 200);
        var service = f.Get<IInvoiceReturnReceivingService>(); await service.ReceiveAsync(1, Command());
        var payment = f.Get<IPaymentService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => payment.CreateAsync(new() {
            InvoiceId = 1, Amount = 101, PaymentMethod = PaymentMethod.Cash, SalesRepId = 1, SalesRepSessionId = 1 }));
        f.Db.ChangeTracker.Clear();
        var debt = Assert.Single(await f.Get<IReceivablesService>().GetCustomerReceivableDetailsAsync(1));
        Assert.Equal(100, debt.OutstandingAmount); Assert.Equal(200, debt.CreditAppliedAmount); Assert.Equal(700, debt.PaidAmount);
        var customers = Assert.Single(await f.Get<IReceivablesService>().GetCustomerReceivablesAsync());
        Assert.Equal(100, customers.OutstandingAmount);
        f.Actor.Role = "SalesRep";
        var summary = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.Equal(100, summary.RemainingTotal);
        Assert.Equal(0, summary.PaidTotal);
        var outstanding = Assert.Single(await payment.GetCurrentSalesRepOutstandingInvoicesAsync());
        Assert.Equal(100, outstanding.OutstandingAmount); Assert.Equal(200, outstanding.CreditAppliedAmount);
        Assert.Empty(await f.Db.Payments.ToListAsync());
    }

    [Fact]
    public async Task Reports_use_posting_date_and_actual_received_value_not_approval_or_requested_amount()
    {
        await using var f = await WorkflowFixture.CreateAsync(); await SeedAsync(f);
        var report = f.Get<IReportQueryService>();
        Assert.Equal(0, await report.GetTotalReturnsAsync([1], DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1)));
        await f.Get<IInvoiceReturnReceivingService>().ReceiveAsync(1, Command(2));
        Assert.Equal(100, await report.GetTotalReturnsAsync([1], DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1)));
        Assert.Equal(0, await report.GetTotalReturnsAsync([2], DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1)));
        Assert.Equal(0, await report.GetTotalReturnsAsync([1], DateTime.UtcNow.Date.AddDays(-1), DateTime.UtcNow.Date));
        var calculation = await f.Get<ISalesRepDayClosingCalculator>().CalculateAsync(
            new() { SalesRepSessionId = 1, WarehouseId = 1 }, await f.Db.SalesRepSessions.SingleAsync());
        Assert.Equal(100, calculation.TotalReturnAmount);
        Assert.Equal(0, calculation.ExpectedCashAmount);
        Assert.Equal(200, calculation.OutstandingAmount);
    }
}
