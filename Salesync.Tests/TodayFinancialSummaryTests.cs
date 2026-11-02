using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using Xunit;

namespace Salesync.Tests;

public class TodayFinancialSummaryTests
{
    [Theory]
    [InlineData(InvoiceStatus.Draft, 1, 150, 120)]
    [InlineData(InvoiceStatus.Cancelled, 1, 150, 120)]
    [InlineData(InvoiceStatus.Confirmed, 2, 650, 610)]
    public async Task Summary_counts_only_confirmed_invoices_but_keeps_document_history(
        InvoiceStatus status, int count, decimal sales, decimal remaining)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        f.Db.AddRange(
            Invoice(1, 1, 1, InvoiceStatus.Confirmed, 150, 30),
            Invoice(2, 1, 1, status, 500, 10));
        await f.Db.SaveChangesAsync();

        var mobile = f.Get<ISalesRepMobileService>();
        var summary = await mobile.GetTodayAsync();

        Assert.Equal(count, summary.InvoicesCount);
        Assert.Equal(sales, summary.SalesTotal);
        Assert.Equal(remaining, summary.RemainingTotal);
        Assert.Equal(0, summary.PaymentsCount);
        // Listing drafts/cancelled documents remains possible; only KPIs change.
        Assert.Equal(2, (await mobile.GetInvoicesAsync(1)).Count());
    }

    [Theory]
    [InlineData(PaymentStatus.Pending, 1, 20)]
    [InlineData(PaymentStatus.PartiallyPaid, 1, 20)]
    [InlineData(PaymentStatus.Overdue, 1, 20)]
    [InlineData(PaymentStatus.Paid, 2, 220)]
    public async Task Summary_includes_paid_collections_of_older_invoices_in_current_session(
        PaymentStatus status, int count, decimal collections)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        var old = await f.SessionAsync(2);
        old.Status = DayStatus.Closed;
        old.EndTime = DateTime.UtcNow;
        await f.SessionAsync(3, 2);
        f.Db.AddRange(
            Invoice(1, 1, 1, InvoiceStatus.Confirmed, 150, 20),
            Invoice(2, 1, 2, InvoiceStatus.Confirmed, 1000, 200),
            Invoice(3, 2, 3, InvoiceStatus.Confirmed, 500, 50));
        f.Db.AddRange(
            Payment(1, 1, 1, 1, 20, PaymentStatus.Paid),
            Payment(2, 2, 1, 1, 200, status),
            Payment(3, 3, 2, 3, 50, PaymentStatus.Paid),
            Payment(4, 2, 1, 2, 70, PaymentStatus.Paid));
        await f.Db.SaveChangesAsync();

        var summary = await f.Get<ISalesRepMobileService>().GetTodayAsync();

        Assert.Equal(count, summary.PaymentsCount);
        Assert.Equal(collections, summary.PaidTotal);
        Assert.Equal(150, summary.SalesTotal);
        Assert.Equal(130, summary.RemainingTotal);
        Assert.Equal(1, summary.InvoicesCount);
        // Today's remaining balance never deducts old-invoice collections.
        if (status == PaymentStatus.Paid)
            Assert.NotEqual(summary.SalesTotal - summary.PaidTotal, summary.RemainingTotal);
    }

    [Fact]
    public async Task Inactive_documents_do_not_change_summary()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync();
        var invoice = Invoice(1, 1, 1, InvoiceStatus.Confirmed, 500, 100);
        invoice.IsActive = false;
        var payment = Payment(1, 1, 1, 1, 100, PaymentStatus.Paid);
        payment.IsActive = false;
        f.Db.AddRange(invoice, payment);
        await f.Db.SaveChangesAsync();

        var summary = await f.Get<ISalesRepMobileService>().GetTodayAsync();

        Assert.Equal(0, summary.InvoicesCount);
        Assert.Equal(0, summary.PaymentsCount);
        Assert.Equal(0, summary.SalesTotal);
        Assert.Equal(0, summary.PaidTotal);
        Assert.Equal(0, summary.RemainingTotal);
    }

    [Fact]
    public async Task Today_review_and_closing_calculate_the_same_session_financial_values()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        var session = await f.SessionAsync();
        session.WorkingDate = f.Get<IBusinessClock>().Today;
        var old = await f.SessionAsync(2);
        old.Status = DayStatus.Closed;
        old.EndTime = DateTime.UtcNow;
        f.Db.AddRange(
            Invoice(1, 1, 1, InvoiceStatus.Confirmed, 150, 30),
            Invoice(2, 1, 1, InvoiceStatus.Draft, 700, 0),
            Invoice(3, 1, 1, InvoiceStatus.Cancelled, 900, 0),
            Invoice(4, 1, 2, InvoiceStatus.Confirmed, 1000, 200));
        f.Db.AddRange(
            Payment(1, 1, 1, 1, 30, PaymentStatus.Paid),
            Payment(2, 4, 1, 1, 200, PaymentStatus.Paid),
            Payment(3, 1, 1, 1, 20, PaymentStatus.Pending));
        await f.Db.SaveChangesAsync();

        var today = await f.Get<ISalesRepMobileService>().GetTodayAsync();
        var review = await f.Get<MobileWorkflowQueryService>().ReviewAsync(1, 1);
        var calculation = await f.Get<ISalesRepDayClosingCalculator>().CalculateAsync(
            new() { SalesRepSessionId = 1, WarehouseId = 1 }, session);
        Assert.True(review.CanClose);
        Assert.Equal(150, today.SalesTotal);
        Assert.Equal(230, today.PaidTotal);
        Assert.Equal(120, today.RemainingTotal);
        Assert.Equal(today.SalesTotal, calculation.TotalSalesAmount);
        Assert.Equal(today.PaidTotal, calculation.TotalCollectionAmount);
        Assert.Equal(today.RemainingTotal, calculation.OutstandingAmount);

        await f.Get<ISalesRepSessionService>().CloseSessionAsync(1, 1);
        var closing = await f.Db.SalesRepDayClosings.AsNoTracking().SingleAsync();
        Assert.Equal(today.SalesTotal, closing.TotalSalesAmount);
        Assert.Equal(today.PaidTotal, closing.TotalCollectionAmount);
        Assert.Equal(today.RemainingTotal, closing.OutstandingAmount);
        Assert.Equal(230, closing.ExpectedCashAmount);
    }

    private static Invoice Invoice(int id, int rep, int session, InvoiceStatus status,
        decimal amount, decimal paid) => new()
    {
        Id = id, InvoiceNumber = $"INV-{id}", CustomerId = 1,
        WarehouseId = rep == 2 ? 2 : 1, SalesRepId = rep,
        SalesRepSessionId = session, TotalAmount = amount, PaidAmount = paid, Status = status
    };

    private static Payment Payment(int id, int invoice, int rep, int session,
        decimal amount, PaymentStatus status) => new()
    {
        Id = id, PaymentNumber = $"PAY-{id}", InvoiceId = invoice, CustomerId = 1,
        SalesRepId = rep, SalesRepSessionId = session, Amount = amount,
        PaymentMethod = PaymentMethod.Cash, Status = status, PaymentDate = DateTime.UtcNow
    };
}
