using Microsoft.EntityFrameworkCore;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Xunit;

namespace Salesync.Tests;

// Synthetic data only. No host configuration, financial commands or external database.
public class CustomerInvoiceOutstandingTests
{
    private static async Task<WorkflowFixture> SeedAsync()
    {
        var f = await WorkflowFixture.CreateAsync();
        await f.SessionAsync(1);
        var old = await f.SessionAsync(2);
        old.EndTime = DateTime.UtcNow;
        old.Status = DayStatus.Closed;
        f.Db.Add(new Route { Id = 1, RouteCode = "OUT-1", Name = "Route", BranchId = 1, AssignedSalesRepId = 1 });
        f.Db.Add(new RouteCustomer { RouteId = 1, CustomerId = 1, VisitSequence = 1 });
        await f.Db.SaveChangesAsync();
        return f;
    }

    private static Invoice Invoice(int id, decimal total, decimal paid = 0m, decimal credit = 0m,
        int? session = 1, int rep = 1, InvoiceStatus status = InvoiceStatus.Confirmed, bool active = true) => new()
    {
        Id = id, InvoiceNumber = $"OUT-{id}", CustomerId = 1, WarehouseId = 1,
        SalesRepId = rep, SalesRepSessionId = session, TotalAmount = total,
        PaidAmount = paid, CreditAppliedAmount = credit, Status = status, IsActive = active
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Both_contracts_include_closed_session_invoices_preserve_master_and_do_not_write(bool routeOnly)
    {
        await using var f = await SeedAsync();
        f.Db.AddRange(Invoice(1, 2800m, 1000m, 193.10m), Invoice(2, 2000m, 1500m, session: 2));
        var customer = await f.Db.Customers.SingleAsync();
        customer.CurrentBalance = 0m;
        await f.Db.SaveChangesAsync();
        f.Db.ChangeTracker.Clear();
        var service = f.Get<ISalesRepMobileService>();
        var result = Assert.Single(routeOnly ? await service.GetRouteCustomersAsync(1, 1) : await service.GetCustomersAsync(1));
        Assert.Equal(2106.90m, result.InvoiceOutstandingAmount);
        Assert.Equal(0m, result.CurrentBalance);
        Assert.Empty(f.Db.ChangeTracker.Entries());
        Assert.Equal(193.10m, (await f.Db.Invoices.AsNoTracking().SingleAsync(x => x.Id == 1)).CreditAppliedAmount);
    }

    [Theory]
    [InlineData(100, 0, 0, 100)]
    [InlineData(100, 40, 0, 60)]
    [InlineData(100, 100, 0, 0)]
    [InlineData(100, 0, 100, 0)]
    [InlineData(100, 40, 20, 40)]
    [InlineData(100, 80, 30, 0)]
    public async Task Positive_remainder_uses_invoice_counters_once(int total, int paid, int credit, int expected)
    {
        await using var f = await SeedAsync();
        f.Db.Add(Invoice(1, total, paid, credit));
        await f.Db.SaveChangesAsync();
        Assert.Equal((decimal)expected, Assert.Single(await f.Get<ISalesRepMobileService>().GetCustomersAsync(1)).InvoiceOutstandingAmount);
    }

    [Fact]
    public async Task Excludes_ineligible_and_other_reps_without_netting_negative_invoices()
    {
        await using var f = await SeedAsync();
        f.Db.AddRange(Invoice(1, 100), Invoice(2, 100, 150),
            Invoice(3, 900, rep: 2), Invoice(4, 900, status: InvoiceStatus.Draft),
            Invoice(5, 900, status: InvoiceStatus.Cancelled), Invoice(6, 900, active: false),
            Invoice(7, 50, session: null));
        await f.Db.SaveChangesAsync();
        Assert.Equal(150m, Assert.Single(await f.Get<ISalesRepMobileService>().GetCustomersAsync(1)).InvoiceOutstandingAmount);
    }

    [Fact]
    public async Task Payment_and_return_records_are_not_subtracted_twice_or_unused_credit_applied()
    {
        await using var f = await SeedAsync();
        f.Db.Add(Invoice(1, 2800m, 1000m, 193.10m));
        f.Db.Add(new Payment { PaymentNumber = "OUT-PAY", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Amount = 1000m, Status = PaymentStatus.Paid });
        f.Db.Add(new InvoiceReturn { Id = 1, ReturnNumber = "OUT-RETURN", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Status = ReturnStatus.ReceivedAtWarehouse, TotalAmount = 293.10m });
        f.Db.Add(new InvoiceReturnReceipt { InvoiceReturnId = 1, InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, OperationId = Guid.NewGuid(), RequestHash = "isolated-test",
            ReceiptNumber = "OUT-RECEIPT", CreditNoteNumber = "OUT-CREDIT", TotalAmount = 293.10m,
            AppliedToInvoiceAmount = 193.10m, CustomerCreditAmount = 100m });
        f.Db.Add(new InvoiceReturn { Id = 2, ReturnNumber = "OUT-PENDING", InvoiceId = 1, CustomerId = 1,
            SalesRepId = 1, SalesRepSessionId = 1, Status = ReturnStatus.Approved, TotalAmount = 500m });
        await f.Db.SaveChangesAsync();
        Assert.Equal(1606.90m, Assert.Single(await f.Get<ISalesRepMobileService>().GetCustomersAsync(1)).InvoiceOutstandingAmount);
    }

    [Fact]
    public async Task No_invoices_is_real_zero_and_repeated_route_membership_does_not_duplicate_debt()
    {
        await using var f = await SeedAsync();
        var service = f.Get<ISalesRepMobileService>();
        Assert.Equal(0m, Assert.Single(await service.GetCustomersAsync(1)).InvoiceOutstandingAmount);
        f.Db.Add(new Route { Id = 2, RouteCode = "OUT-2", Name = "Second", BranchId = 1, AssignedSalesRepId = 1 });
        f.Db.Add(new RouteCustomer { RouteId = 2, CustomerId = 1, VisitSequence = 1 });
        f.Db.Add(Invoice(1, 100));
        await f.Db.SaveChangesAsync();
        var results = (await service.GetCustomersAsync(1)).ToList();
        Assert.Equal(2, results.Count);
        Assert.All(results, x => Assert.Equal(100m, x.InvoiceOutstandingAmount));
    }

    [Fact]
    public async Task Batch_groups_each_customer_independently_including_metro_market_example()
    {
        await using var f = await SeedAsync();
        f.Db.Add(new Salesync.Domain.Modules.MasterData.Entities.Customer
            { Id = 2, Name = "Metro Market", CurrentBalance = 0m });
        f.Db.Add(new RouteCustomer { RouteId = 1, CustomerId = 2, VisitSequence = 2 });
        f.Db.Add(Invoice(1, 750m));
        var current = Invoice(2, 2800m, 1000m, 193.10m);
        var old = Invoice(3, 2000m, 1500m, session: 2);
        current.CustomerId = old.CustomerId = 2;
        f.Db.AddRange(current, old);
        await f.Db.SaveChangesAsync();
        var service = f.Get<ISalesRepMobileService>();
        foreach (var rows in new[] { await service.GetCustomersAsync(1), await service.GetRouteCustomersAsync(1, 1) })
        {
            Assert.Equal(750m, Assert.Single(rows, x => x.CustomerId == 1).InvoiceOutstandingAmount);
            var metro = Assert.Single(rows, x => x.CustomerId == 2);
            Assert.Equal(2106.90m, metro.InvoiceOutstandingAmount);
            Assert.Equal(0m, metro.CurrentBalance);
        }
    }

    [Fact]
    public async Task Customer_and_session_authorization_boundaries_remain_enforced()
    {
        await using var f = await SeedAsync();
        await f.SessionAsync(3, rep: 2);
        f.Db.Add(new Route { Id = 3, RouteCode = "OTHER", Name = "Other rep", BranchId = 2, AssignedSalesRepId = 2 });
        f.Db.Add(new RouteCustomer { RouteId = 3, CustomerId = 1, VisitSequence = 1 });
        f.Db.AddRange(Invoice(1, 100), Invoice(2, 999, rep: 2, session: 3));
        await f.Db.SaveChangesAsync();
        var service = f.Get<ISalesRepMobileService>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetCustomersAsync(3));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetRouteCustomersAsync(1, 3));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCustomersAsync(2)); // Closed read guard unchanged.
        f.Actor.UserId = "rep-2"; // DB identity, not the fixture's fixed SalesRepId claim.
        Assert.Equal(999m, Assert.Single(await service.GetRouteCustomersAsync(3, 3)).InvoiceOutstandingAmount);
    }
}
