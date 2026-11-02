using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Xunit;

namespace Salesync.Tests;

// Audit and Fix 1 regressions: synthetic records in WorkflowFixture's in-memory SQLite.
// Never starts the API, loads host settings or invokes a financial command.
public class DataIntegrityAuditTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [Trait("Audit", "Characterization")]
    public async Task Master_customer_balance_and_invoice_receivables_are_distinct_sources(int masterBalance)
    {
        await using var f = await SeedAsync();
        var customer = await f.Db.Customers.SingleAsync();
        customer.CurrentBalance = masterBalance;
        await f.Db.SaveChangesAsync();

        var mobile = f.Get<ISalesRepMobileService>();
        var routeCustomer = Assert.Single(await mobile.GetRouteCustomersAsync(1, 1));
        var allCustomer = Assert.Single(await mobile.GetCustomersAsync(1));
        var receivable = Assert.Single(await f.Get<IReceivablesService>().GetCustomerReceivablesAsync());

        Assert.Equal((decimal)masterBalance, routeCustomer.CurrentBalance);
        Assert.Equal(routeCustomer.CurrentBalance, allCustomer.CurrentBalance);
        Assert.Equal(2106.90m, receivable.OutstandingAmount); // 500 old + 1606.90 current.
        Assert.NotEqual(receivable.OutstandingAmount, routeCustomer.CurrentBalance);
        Assert.Equal(1606.90m, (await mobile.GetTodayAsync()).RemainingTotal); // Session scope.
    }

    [Fact]
    [Trait("Audit", "Characterization")]
    public async Task Closed_session_snapshot_and_receipt_based_review_agree_for_completed_workday()
    {
        await using var f = await SeedAsync();
        var session = await f.Db.SalesRepSessions.SingleAsync(x => x.Id == 1);
        // Reproduce an already-closed snapshot, without executing CloseSession.
        session.Status = DayStatus.Closed;
        session.EndTime = DateTime.UtcNow;
        session.TotalReturnAmount = 193.10m;
        session.NetSales = 2606.90m;
        await f.Db.SaveChangesAsync();

        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        var archive = await f.Get<MobileWorkflowQueryService>().SessionsAsync(null, null, 1, 100);

        Assert.True(today.IsDayClosed);
        Assert.Equal(193.10m, today.Session!.TotalReturnAmount);
        Assert.Equal(193.10m, today.ReceivedReturnAmount);
        Assert.Equal(today.Session.TotalReturnAmount, review.FinancialSummary.TotalReturnAmount);
        Assert.Equal(193.10m, Assert.Single(archive.Items, x => x.Id == 1).TotalReturnAmount);
        Assert.Equal(1606.90m, today.RemainingTotal);
        Assert.Equal(1000m, today.PaidTotal); // Credit must never masquerade as cash.
    }

    [Fact]
    [Trait("Audit", "Characterization")]
    public async Task Received_return_total_uses_posting_session_not_original_invoice_or_request_session()
    {
        await using var f = await SeedAsync();
        var invoice = await f.Db.Invoices.SingleAsync(x => x.Id == 1);
        var request = await f.Db.InvoiceReturns.SingleAsync();
        invoice.SalesRepSessionId = 2;
        request.SalesRepSessionId = 2;
        // Receipt remains assigned to posting session 1. No receiving command is run.
        await f.Db.SaveChangesAsync();
        var query = f.Get<MobileWorkflowQueryService>();
        Assert.Equal(193.10m, (await query.ReviewAsync(1, 1)).FinancialSummary.TotalReturnAmount);
        Assert.Equal(0m, (await query.ReviewAsync(2, 1)).FinancialSummary.TotalReturnAmount);
        Assert.Equal(193.10m, (await f.Get<ISalesRepMobileService>().GetTodayAsync()).ReceivedReturnAmount);
    }

    [Fact]
    [Trait("Audit", "Regression")]
    public async Task Open_session_details_should_expose_received_return_total_without_waiting_for_closing()
    {
        await using var f = await SeedAsync();
        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        Assert.Equal(193.10m, review.FinancialSummary.TotalReturnAmount);
        Assert.Equal(review.FinancialSummary.TotalReturnAmount, today.ReceivedReturnAmount);
        Assert.Equal(0m, today.Session!.TotalReturnAmount);
        Assert.Equal(0m, (await f.Db.SalesRepSessions.AsNoTracking().SingleAsync(x => x.Id == 1)).TotalReturnAmount);
        Assert.Equal(1000m, today.PaidTotal);
        Assert.Equal(2800m, today.SalesTotal);
        Assert.Equal(1606.90m, today.RemainingTotal);
    }

    [Fact]
    public async Task Closed_snapshot_is_never_recalculated_or_overwritten_by_live_receipt_reads()
    {
        await using var f = await SeedAsync();
        var session = await f.Db.SalesRepSessions.SingleAsync(x => x.Id == 1);
        session.Status = DayStatus.Closed;
        session.EndTime = DateTime.UtcNow;
        session.TotalReturnAmount = 17m; // Deliberately distinct historical snapshot.
        session.NetSales = 2783m;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();

        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        var saved = await f.Db.SalesRepSessions.AsNoTracking().SingleAsync(x => x.Id == 1);
        Assert.True(today.IsDayClosed);
        Assert.Equal(193.10m, today.ReceivedReturnAmount);
        Assert.Equal(17m, today.Session!.TotalReturnAmount);
        Assert.Equal(17m, saved.TotalReturnAmount);
        Assert.Equal(2783m, saved.NetSales);
        Assert.DoesNotContain(f.Db.ChangeTracker.Entries(), e => e.State is EntityState.Modified or EntityState.Added or EntityState.Deleted);
    }

    [Fact]
    public async Task Live_total_matches_calculator_and_excludes_inactive_other_session_and_unassigned_receipts()
    {
        await using var f = await SeedAsync();
        InvoiceReturnReceipt Receipt(string reference, int? postingSession, bool active, decimal amount) => new() {
            InvoiceReturnId = 1, InvoiceId = 1, CustomerId = 1, SalesRepId = 1, SalesRepSessionId = postingSession,
            OperationId = Guid.NewGuid(), RequestHash = "audit-only", ReceiptNumber = reference, CreditNoteNumber = reference + "-CN",
            TotalAmount = amount, AppliedToInvoiceAmount = amount, IsActive = active
        };
        f.Db.AddRange(Receipt("INACTIVE", 1, false, 500m), Receipt("OTHER-SESSION", 2, true, 700m), Receipt("UNASSIGNED", null, true, 900m));
        await f.Db.SaveChangesAsync();
        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        Assert.Equal(193.10m, today.ReceivedReturnAmount);
        Assert.Equal(today.ReceivedReturnAmount, review.FinancialSummary.TotalReturnAmount);
    }

    [Theory]
    [InlineData(ReturnStatus.Pending)]
    [InlineData(ReturnStatus.Approved)]
    public async Task A_return_request_without_a_posted_receipt_has_zero_received_value(ReturnStatus status)
    {
        await using var f = await SeedAsync(withReceipt: false);
        (await f.Db.InvoiceReturns.SingleAsync()).Status = status;
        await f.Db.SaveChangesAsync();
        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.Equal(0m, today.ReceivedReturnAmount);
        Assert.Equal(0m, today.Session!.TotalReturnAmount);
        Assert.Equal(1000m, today.PaidTotal);
        Assert.Equal(1800m, today.RemainingTotal);
    }

    [Fact]
    public async Task No_selected_session_returns_zero_without_a_snapshot_or_financial_commands()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        Assert.Null(today.Session);
        Assert.Equal(0m, today.ReceivedReturnAmount);
    }

    private static async Task<WorkflowFixture> SeedAsync(bool withReceipt = true)
    {
        var f = await WorkflowFixture.CreateAsync();
        var current = await f.SessionAsync();
        current.WorkingDate = f.Get<IBusinessClock>().Today;
        var old = await f.SessionAsync(2);
        old.Status = DayStatus.Closed;
        old.EndTime = DateTime.UtcNow.AddDays(-1);
        f.Db.Add(new Route { Id = 1, RouteCode = "AUDIT", Name = "Audit route", BranchId = 1, AssignedSalesRepId = 1 });
        f.Db.Add(new RouteCustomer { RouteId = 1, CustomerId = 1, VisitSequence = 1 });
        f.Db.AddRange(
            new Invoice { Id = 1, InvoiceNumber = "AUDIT-CURRENT", CustomerId = 1, WarehouseId = 1,
                SalesRepId = 1, SalesRepSessionId = 1, Status = InvoiceStatus.Confirmed,
                SubTotal = 3000m, DiscountAmount = 200m, TotalAmount = 2800m, PaidAmount = 1000m, CreditAppliedAmount = withReceipt ? 193.10m : 0m },
            new Invoice { Id = 2, InvoiceNumber = "AUDIT-OLD", CustomerId = 1, WarehouseId = 1,
                SalesRepId = 1, SalesRepSessionId = 2, Status = InvoiceStatus.Confirmed,
                TotalAmount = 2000m, PaidAmount = 1500m });
        f.Db.Add(new Payment { PaymentNumber = "AUDIT-CASH", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Amount = 1000m, PaymentMethod = PaymentMethod.Cash, Status = PaymentStatus.Paid });
        f.Db.Add(new InvoiceReturn { Id = 1, ReturnNumber = "AUDIT-RETURN", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Status = withReceipt ? ReturnStatus.ReceivedAtWarehouse : ReturnStatus.Approved, TotalAmount = 193.10m });
        if (withReceipt) f.Db.Add(new InvoiceReturnReceipt { InvoiceReturnId = 1, InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, OperationId = Guid.NewGuid(), RequestHash = "audit-only",
            ReceiptNumber = "AUDIT-RECEIPT", CreditNoteNumber = "AUDIT-CREDIT",
            TotalAmount = 193.10m, AppliedToInvoiceAmount = 193.10m, CustomerCreditAmount = 0m });
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        return f;
    }
}
