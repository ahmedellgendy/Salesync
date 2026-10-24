using Microsoft.EntityFrameworkCore;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Xunit;

namespace Salesync.Tests;
public class FailureRecoveryTests
{
    [Fact] public async Task Failure_after_session_save_rolls_back_entire_closing_and_retry_recovers()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync(); f.Db.FailClosingInsert=true;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<ISalesRepSessionService>().CloseSessionAsync(1,1));
        var session=await f.Db.SalesRepSessions.AsNoTracking().SingleAsync();
        Assert.Equal(DayStatus.Started,session.Status); Assert.Null(session.EndTime); Assert.False(session.IsStockSettled);
        Assert.Empty(await f.Db.SalesRepDayClosings.ToListAsync());
        f.Db.FailClosingInsert=false; await f.Get<ISalesRepSessionService>().CloseSessionAsync(1,1);
        Assert.Single(await f.Db.SalesRepDayClosings.ToListAsync());
    }
    [Fact] public async Task Failure_after_receipt_insert_rolls_back_cash_balance_receipt_and_session()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var session=await f.SessionAsync(settled:true);
        session.Status=DayStatus.Closed; session.EndTime=DateTime.UtcNow;
        f.Db.Add(new SalesRepDayClosing{Id=1,ClosingNumber="DC",SalesRepId=1,SalesRepSessionId=1,WarehouseId=1,
            Status=SalesRepDayClosingStatus.Submitted,IsStockReceived=true,ExpectedCashAmount=100});
        f.Db.Add(new CashBox{Id=1,Code="CB",Name="Cash",BranchId=1,CurrentBalance=20}); await f.Db.SaveChangesAsync();
        f.Actor.Role="Treasury"; f.Actor.UserId="treasury"; f.Db.FailTreasuryAudit=true;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<ITreasuryService>().ReceiveDayClosingCashAsync(1,new ReceiveDayClosingCashDto{CashBoxId=1,ReceivedAmount=90}));
        Assert.Equal(20,(await f.Db.CashBoxes.SingleAsync()).CurrentBalance);
        Assert.Empty(await f.Db.CashReceipts.ToListAsync()); Assert.Empty(await f.Db.TreasuryTransactions.ToListAsync());
        Assert.Empty(await f.Db.SalesRepCashLedgers.ToListAsync()); Assert.False((await f.Db.SalesRepSessions.SingleAsync()).IsTreasurySettled);
        Assert.False((await f.Db.SalesRepDayClosings.SingleAsync()).IsCashReceived);
    }
    [Fact] public async Task Open_visit_blocks_closing_in_backend_without_mobile_preflight()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync();
        f.Db.Add(new Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit{SalesRepId=1,CustomerId=1,SalesRepSessionId=1,Status=VisitStatus.InProgress});
        await f.Db.SaveChangesAsync();
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<ISalesRepSessionService>().CloseSessionAsync(1,1));
        Assert.Contains("OPEN_VISITS",error.Message); Assert.Empty(await f.Db.SalesRepDayClosings.ToListAsync());
    }
    [Fact] public async Task Mobile_invoice_failure_after_confirmation_rolls_back_invoice_inventory_movement_and_visit()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync();
        f.Db.Add(new Product{Id=1,ItemCode="P1",Name="Product",UnitsPerLargeUnit=24,UnitPrice=10});
        f.Db.Add(new Route{Id=1,RouteCode="R1",Name="Route",BranchId=1,AssignedSalesRepId=1});
        f.Db.Add(new RouteCustomer{RouteId=1,CustomerId=1});
        f.Db.Add(new SalesRepInventory{SalesRepId=1,ProductId=1,Quantity=48});
        f.Db.Add(new Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit{Id=1,SalesRepId=1,CustomerId=1,SalesRepSessionId=1,RouteId=1,Status=VisitStatus.InProgress});
        await f.Db.SaveChangesAsync(); f.Db.ChangeTracker.Clear(); f.Db.FailVisitCompletion=true;
        var command=new CreateSalesRepMobileInvoiceDto{VisitId=1,CustomerId=1,SalesRepSessionId=1,
            Items=[new(){ProductId=1,SaleLargeQuantity=1}]};
        var error=await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<ISalesRepMobileService>().CreateInvoiceAsync(command));
        Assert.Contains("Injected visit",error.Message);
        Assert.Empty(await f.Db.Invoices.ToListAsync()); Assert.Empty(await f.Db.SalesRepInventoryMovements.ToListAsync());
        Assert.Equal(48,(await f.Db.SalesRepInventories.SingleAsync()).Quantity);
        Assert.Null((await f.Db.CustomerVisits.SingleAsync()).InvoiceId);
        f.Db.FailVisitCompletion=false; await f.Get<ISalesRepMobileService>().CreateInvoiceAsync(command);
        Assert.Single(await f.Db.Invoices.ToListAsync()); Assert.Equal(24,(await f.Db.SalesRepInventories.SingleAsync()).Quantity);
        Assert.Equal(VisitStatus.Completed,(await f.Db.CustomerVisits.SingleAsync()).Status);
    }
}
