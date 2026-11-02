using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Infrastructure.Modules.Identity.Entities;
using Xunit;

namespace Salesync.Tests;

public sealed class ReviewSession25DiagnosticsTests
{
    private static async Task PrepareRep5Async(WorkflowFixture fixture)
    {
        fixture.Actor.UserId = "diagnostic-rep-5";
        fixture.Db.Add(new ApplicationUser { Id = fixture.Actor.UserId, FullName = "Local test representative" });
        fixture.Db.Add(new SalesRep { Id = 5, SalesRepCode = "TEST5", Name = "Local test", Phone = "5",
            BranchId = 1, UserId = fixture.Actor.UserId });
        await fixture.Db.SaveChangesAsync();
        var old = await fixture.SessionAsync(25, 5);
        old.WorkingDate = new DateTime(2026, 10, 26);
        await fixture.Db.SaveChangesAsync();
        fixture.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Explicit_review_keeps_rep5_session25_and_only_reads_financial_state()
    {
        await using var fixture = await WorkflowFixture.CreateAsync();
        await PrepareRep5Async(fixture);
        await fixture.SessionAsync(99, 5);
        fixture.Db.ChangeTracker.Clear();

        var result = await fixture.Get<MobileWorkflowQueryService>().ReviewAsync(25, 1);

        Assert.Equal(25, result.Session.Id);
        Assert.Equal(5, result.Session.SalesRepId);
        Assert.Equal(new DateTime(2026, 10, 26), result.Session.WorkingDate);
        Assert.Equal(1, result.WarehouseId);
        Assert.Contains(result.Checks, check => check.Code == "PENDING_RETURNS");
        Assert.Equal(0, result.FinancialSummary.TotalReturnAmount);
        Assert.Empty(await fixture.Db.Set<OperationExecution>().ToListAsync());
        Assert.Empty(await fixture.Db.SalesRepDayClosings.ToListAsync());
        Assert.False(fixture.Db.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task Missing_receipt_table_breaks_live_today_and_review_in_isolated_database()
    {
        // This fixture owns a disposable in-memory SQLite database. No production
        // connection, configuration, migration runner or API Host is involved.
        await using var fixture = await WorkflowFixture.CreateAsync();
        await PrepareRep5Async(fixture);
        var today = await fixture.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.Equal(25, today.Session!.Id);
        Assert.Equal(0m, today.ReceivedReturnAmount);
        await fixture.Db.Database.ExecuteSqlRawAsync("DROP TABLE InvoiceReturnReceipts");

        // Today now reads live receipts too. Missing schema must not be hidden
        // by substituting a zero or the old persisted session snapshot.
        var todayError = await Assert.ThrowsAsync<SqliteException>(
            () => fixture.Get<ISalesRepMobileService>().GetTodayAsync());
        Assert.Contains("InvoiceReturnReceipts", todayError.Message);

        var error = await Assert.ThrowsAsync<SqliteException>(
            () => fixture.Get<MobileWorkflowQueryService>().ReviewAsync(25, 1));
        Assert.Contains("InvoiceReturnReceipts", error.Message);
        Assert.Empty(await fixture.Db.Set<OperationExecution>().ToListAsync());
        Assert.Empty(await fixture.Db.SalesRepDayClosings.ToListAsync());
        Assert.False(fixture.Db.ChangeTracker.HasChanges());
        // Reproduces a possible dependency failure, not proof about Production.
    }
}
