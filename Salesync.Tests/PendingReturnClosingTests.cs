using Microsoft.EntityFrameworkCore;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.Sales.Entities;
using Xunit;

namespace Salesync.Tests;

public class PendingReturnClosingTests
{
    [Theory]
    [InlineData("SalesRep")]
    [InlineData("Admin")]
    public async Task Direct_close_rejects_pending_return_without_changing_session_or_creating_settlement(string role)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        await SeedReturnAsync(f);
        f.Actor.Role = role;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Get<ISalesRepSessionService>().CloseSessionAsync(1, 1));

        Assert.Contains("PENDING_RETURNS", error.Message);
        var session = await f.Db.SalesRepSessions.AsNoTracking().SingleAsync();
        Assert.Equal(DayStatus.Started, session.Status);
        Assert.Null(session.EndTime);
        Assert.False(session.IsStockSettled);
        Assert.Empty(await f.Db.SalesRepDayClosings.ToListAsync());
    }

    [Fact]
    public async Task Review_returns_pending_check_and_blocks_readiness_without_writes()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        await SeedReturnAsync(f);

        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);

        Assert.False(review.CanClose);
        var check = Assert.Single(review.Checks, x => x.Code == "PENDING_RETURNS");
        Assert.False(check.Passed);
        Assert.Contains("الإدارة", check.Title);
        Assert.All(review.Checks.Where(x => x.Code != "PENDING_RETURNS"), x => Assert.True(x.Passed));
        Assert.Equal(ReturnStatus.Pending, (await f.Db.InvoiceReturns.SingleAsync()).Status);
        Assert.Empty(await f.Db.SalesRepDayClosings.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Administration_must_resolve_return_before_representative_can_close(bool approve)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        await SeedReturnAsync(f);
        var close = f.Get<ISalesRepSessionService>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => close.CloseSessionAsync(1, 1));

        f.Actor.Role = "Admin";
        var returns = f.Get<IInvoiceReturnService>();
        if (approve) await returns.ApproveAsync(1);
        else await returns.RejectAsync(1);
        f.Actor.Role = "SalesRep";
        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        Assert.True(review.CanClose);
        Assert.True(Assert.Single(review.Checks, x => x.Code == "PENDING_RETURNS").Passed);

        await close.CloseSessionAsync(1, 1);
        await close.CloseSessionAsync(1, 1);
        var closing = Assert.Single(await f.Db.SalesRepDayClosings.ToListAsync());
        Assert.Equal(0m, closing.TotalReturnAmount);
        Assert.Empty(await f.Db.InvoiceReturnReceipts.ToListAsync());
        // No credit posting is introduced by the pending-return closing rule.
        Assert.Equal(100m, (await f.Db.Invoices.SingleAsync()).TotalAmount);
        Assert.Empty(await f.Db.SalesRepInventories.ToListAsync());
    }

    [Theory]
    [InlineData(ReturnStatus.Approved, true)]
    [InlineData(ReturnStatus.Rejected, true)]
    [InlineData(ReturnStatus.ReceivedAtWarehouse, true)]
    [InlineData(ReturnStatus.Cancelled, true)]
    [InlineData(ReturnStatus.Pending, false)]
    public async Task Only_active_pending_returns_block_the_session(ReturnStatus status, bool active)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        await SeedReturnAsync(f, status, active);

        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        Assert.True(review.CanClose);
        await f.Get<ISalesRepSessionService>().CloseSessionAsync(1, 1);
        Assert.Single(await f.Db.SalesRepDayClosings.ToListAsync());
    }

    [Fact]
    public async Task Pending_return_of_another_session_does_not_block_current_day()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        await f.SessionAsync(2);
        await SeedReturnAsync(f, sessionId: 2);

        Assert.True((await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1)).CanClose);
        await f.Get<ISalesRepSessionService>().CloseSessionAsync(1, 1);
        Assert.Single(await f.Db.SalesRepDayClosings.ToListAsync());
        Assert.Equal(ReturnStatus.Pending, (await f.Db.InvoiceReturns.SingleAsync()).Status);
    }

    [Fact]
    public async Task Legacy_closed_session_cannot_bypass_check_by_creating_settlement_directly()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        var session = await f.SessionAsync(settled: true);
        session.Status = DayStatus.Closed;
        session.EndTime = DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        await SeedReturnAsync(f);
        f.Actor.Role = "Admin";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Get<ISalesRepDayClosingService>().CreateAsync(new() { SalesRepSessionId = 1, WarehouseId = 1 }));

        Assert.Contains("PENDING_RETURNS", error.Message);
        Assert.Empty(await f.Db.SalesRepDayClosings.ToListAsync());
        Assert.Empty(await f.Db.CashReceipts.ToListAsync());
    }

    private static async Task SeedReturnAsync(WorkflowFixture f,
        ReturnStatus status = ReturnStatus.Pending, bool active = true, int sessionId = 1)
    {
        f.Db.Add(new Product { Id = 1, ItemCode = "P1", Name = "Product", UnitsPerLargeUnit = 24, UnitPrice = 10 });
        f.Db.Add(new Invoice { Id = 1, InvoiceNumber = "INV1", CustomerId = 1, WarehouseId = 1,
            SalesRepId = 1, SalesRepSessionId = sessionId, TotalAmount = 100, Status = InvoiceStatus.Confirmed });
        f.Db.Add(new InvoiceItem { Id = 1, InvoiceId = 1, ProductId = 1,
            ProductName = "Product", ItemCode = "P1", Quantity = 10, UnitPrice = 10, NetAmount = 100 });
        f.Db.Add(new InvoiceReturn { Id = 1, ReturnNumber = "RET1", InvoiceId = 1,
            CustomerId = 1, SalesRepId = 1, SalesRepSessionId = sessionId,
            Status = status, IsActive = active, TotalAmount = 10 });
        f.Db.Add(new InvoiceReturnItem { InvoiceReturnId = 1, InvoiceItemId = 1, ProductId = 1,
            ProductName = "Product", ItemCode = "P1", Quantity = 1, UnitPrice = 10,
            TotalAmount = 10, Condition = ReturnCondition.Good });
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
    }
}
