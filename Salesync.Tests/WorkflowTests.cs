using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Exceptions;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Application.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Xunit;

namespace Salesync.Tests;

public class WorkflowTests
{
    [Fact] public void Fingerprint_ignores_object_order_and_numeric_format_but_preserves_array_order()
    {
        Assert.Equal(OperationFingerprint.Compute("POST /x", "{\"b\":1.0,\"a\":2}"), OperationFingerprint.Compute("POST /x", "{\"a\":2,\"b\":1}"));
        Assert.NotEqual(OperationFingerprint.Compute("POST /x", "[1,2]"), OperationFingerprint.Compute("POST /x", "[2,1]"));
    }

    [Theory] [InlineData(null,"SalesRep")] [InlineData("rep-1","Unknown")]
    public async Task Missing_authentication_or_unrecognized_role_is_rejected(string? user,string role)
    {
        await using var f=await WorkflowFixture.CreateAsync(); f.Actor.UserId=user; f.Actor.Role=role;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Get<ISalesAccessScopeService>().GetAsync());
    }
    [Fact] public async Task Representative_cannot_access_another_rep_or_warehouse()
    {
        await using var f = await WorkflowFixture.CreateAsync();
        var access=f.Get<ISalesAccessScopeService>();
        Assert.Equal(new[]{1}, (await access.GetAsync()).SalesRepIds);
        await Assert.ThrowsAsync<ForbiddenException>(()=>access.RequireSalesRepAsync(2));
        await Assert.ThrowsAsync<ForbiddenException>(()=>access.RequireWarehouseAsync(2));
        await access.RequireWarehouseAsync(1);
    }
    [Fact] public async Task Supervisor_scope_uses_current_team()
    {
        await using var f=await WorkflowFixture.CreateAsync(); f.Actor.UserId="supervisor"; f.Actor.Role="Supervisor";
        var access=f.Get<ISalesAccessScopeService>();
        Assert.Equal(new[]{1},(await access.GetAsync()).SalesRepIds);
        await Assert.ThrowsAsync<ForbiddenException>(()=>access.RequireSalesRepAsync(2));
    }
    [Fact] public async Task Nested_commit_does_not_commit_outer_transaction()
    {
        await using var f=await WorkflowFixture.CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Unit.ExecuteInTransactionAsync(async()=>{
            await f.Unit.BeginTransactionAsync();
            f.Db.Add(new CashBox{ Code="ROLLBACK",Name="Rollback",BranchId=1,CurrentBalance=50 });
            await f.Unit.CompleteAsync(); await f.Unit.CommitTransactionAsync();
            throw new InvalidOperationException("Injected failure after inner commit");
        }));
        Assert.False(await f.Db.CashBoxes.AnyAsync());
        Assert.Null(f.Db.Database.CurrentTransaction);
    }
    [Fact] public async Task Empty_stock_day_requires_authorized_warehouse_and_closes_once()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync();
        var service=f.Get<ISalesRepSessionService>();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.CloseSessionAsync(1));
        Assert.Null((await f.Db.SalesRepSessions.SingleAsync()).EndTime);
        await Assert.ThrowsAsync<ForbiddenException>(()=>service.CloseSessionAsync(1,2));
        await service.CloseSessionAsync(1,1); await service.CloseSessionAsync(1,1);
        Assert.Equal(1,await f.Db.SalesRepDayClosings.CountAsync());
        Assert.Empty(await f.Db.SalesRepUnloadRequests.ToListAsync());
        var session=await f.Db.SalesRepSessions.SingleAsync();
        Assert.Equal(DayStatus.Closed,session.Status); Assert.True(session.IsStockSettled); Assert.True(session.IsTreasurySettled);
    }
    [Fact] public async Task Closed_session_without_closing_is_recovered()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var s=await f.SessionAsync();
        s.Status=DayStatus.Closed; s.EndTime=DateTime.UtcNow.AddMinutes(-10); var end=s.EndTime;
        await f.Db.SaveChangesAsync(); await f.Get<ISalesRepSessionService>().CloseSessionAsync(1,1);
        Assert.Equal(end,(await f.Db.SalesRepSessions.SingleAsync()).EndTime);
        Assert.Single(await f.Db.SalesRepDayClosings.ToListAsync());
    }
    [Fact] public async Task Payment_after_stock_settlement_of_old_invoice_uses_current_session_and_rejects_overpayment()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync(1,settled:true);
        f.Db.Add(new Invoice{Id=1,InvoiceNumber="OLD",CustomerId=1,WarehouseId=1,SalesRepId=1,TotalAmount=100,Status=InvoiceStatus.Confirmed});
        await f.Db.SaveChangesAsync();
        var payments=f.Get<IPaymentService>();
        var receipt=await payments.CreateAsync(new CreatePaymentDto{ InvoiceId=1,Amount=60,PaymentMethod=PaymentMethod.Cash });
        Assert.Equal(1,receipt.SalesRepSessionId);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>payments.CreateAsync(new CreatePaymentDto{InvoiceId=1,Amount=50,PaymentMethod=PaymentMethod.Cash}));
        Assert.Equal(60,(await f.Db.Invoices.SingleAsync()).PaidAmount);
        Assert.Single(await f.Db.Payments.ToListAsync());
    }
    [Fact] public async Task Session_history_pagination_counts_only_own_records()
    {
        await using var f=await WorkflowFixture.CreateAsync();
        await f.SessionAsync(1); await f.SessionAsync(2); await f.SessionAsync(3,2);
        var page=await f.Get<MobileWorkflowQueryService>().SessionsAsync(null,null,2,1);
        Assert.Single(page.Items); Assert.Equal(2,page.TotalCount); Assert.Equal(2,page.TotalPages); Assert.Equal(2,page.PageNumber);
        Assert.All(page.Items,s=>Assert.Equal(1,s.SalesRepId));
    }
    [Fact] public async Task Attention_is_server_paged_and_scoped_even_with_empty_sections()
    {
        await using var f=await WorkflowFixture.CreateAsync(); f.Actor.Role="Supervisor"; f.Actor.UserId="supervisor";
        var data=await f.Get<MobileWorkflowQueryService>().AttentionAsync(null,null,1,10);
        Assert.Equal(0,data.TotalCount); Assert.Equal(0,data.TotalPages); Assert.Empty(data.Items);
    }
    [Fact] public async Task Legacy_cash_receiving_requires_cash_box_before_any_write()
    {
        await using var f=await WorkflowFixture.CreateAsync();
        var ex=await Assert.ThrowsAsync<ArgumentException>(()=>f.Get<ISalesRepDayClosingService>().ReceiveCashAsync(1,new ReceiveSalesRepDayClosingCashDto{ActualCashAmount=10}));
        Assert.Contains("CASHBOX_REQUIRED",ex.Message); Assert.Empty(await f.Db.CashReceipts.ToListAsync());
    }
    [Fact] public async Task Treasury_receipt_balance_audit_and_closing_are_atomic_and_received_once()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var session=await f.SessionAsync(settled:true);
        session.Status=DayStatus.Closed; session.EndTime=DateTime.UtcNow;
        f.Db.Add(new SalesRepDayClosing{Id=1,ClosingNumber="DC1",SalesRepId=1,SalesRepSessionId=1,WarehouseId=1,
            Status=SalesRepDayClosingStatus.Submitted,IsStockReceived=true,ExpectedCashAmount=100});
        f.Db.Add(new CashBox{Id=1,Code="CB1",Name="Cash",BranchId=1,CurrentBalance=20}); await f.Db.SaveChangesAsync();
        f.Actor.UserId="treasury"; f.Actor.Role="Treasury";
        var service=f.Get<ITreasuryService>();
        await service.ReceiveDayClosingCashAsync(1,new ReceiveDayClosingCashDto{CashBoxId=1,ReceivedAmount=90});
        await Assert.ThrowsAsync<InvalidOperationException>(()=>service.ReceiveDayClosingCashAsync(1,new ReceiveDayClosingCashDto{CashBoxId=1,ReceivedAmount=90}));
        Assert.Equal(110,(await f.Db.CashBoxes.SingleAsync()).CurrentBalance);
        Assert.Single(await f.Db.CashReceipts.ToListAsync()); Assert.Single(await f.Db.TreasuryTransactions.ToListAsync());
        Assert.Equal(-10,(await f.Db.SalesRepCashLedgers.SingleAsync()).Amount);
        Assert.True((await f.Db.SalesRepSessions.SingleAsync()).IsTreasurySettled);
    }
}
