using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Exceptions;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Application.Modules.LoadRequest.Interfaces;
using Salesync.Application.Modules.UnloadRequest.Interfaces;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Sales;
using Salesync.Domain.Common.Enums.Sales.SalesRepDayClosing;
using Salesync.Domain.Common.Enums.CustomerVisit;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.LoadRequest.Entities;
using Salesync.Domain.Modules.UnloadRequest.Entities;
using Xunit;

namespace Salesync.Tests;
public class QueryAndScopeTests
{
    [Fact] public async Task Payments_totals_include_old_invoice_collections_across_all_pages_and_exclude_other_reps()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync(); await f.SessionAsync(2,2);
        f.Db.AddRange(new Invoice{Id=1,InvoiceNumber="OLD",CustomerId=1,WarehouseId=1,SalesRepId=1,TotalAmount=100,Status=InvoiceStatus.Confirmed},
            new Invoice{Id=2,InvoiceNumber="OTHER",CustomerId=1,WarehouseId=2,SalesRepId=2,TotalAmount=100,Status=InvoiceStatus.Confirmed});
        f.Db.AddRange(new Payment{PaymentNumber="P1",InvoiceId=1,CustomerId=1,SalesRepId=1,SalesRepSessionId=1,Amount=30,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Paid},
            new Payment{PaymentNumber="P2",InvoiceId=1,CustomerId=1,SalesRepId=1,SalesRepSessionId=1,Amount=20,PaymentMethod=PaymentMethod.BankTransfer,Status=PaymentStatus.Paid},
            new Payment{PaymentNumber="P3",InvoiceId=2,CustomerId=1,SalesRepId=2,SalesRepSessionId=2,Amount=90,PaymentMethod=PaymentMethod.Cash,Status=PaymentStatus.Paid});
        await f.Db.SaveChangesAsync();
        var data=await f.Get<MobileWorkflowQueryService>().PaymentsAsync(1,null,null,2,1);
        Assert.Single(data.Items); Assert.Equal(2,data.TotalCount); Assert.Equal(2,data.TotalPages); Assert.Equal(2,data.PageNumber);
        Assert.Equal(50,data.Totals.TotalAmount); Assert.Equal(30,data.Totals.CashAmount); Assert.Equal(20,data.Totals.NonCashAmount);
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>f.Get<MobileWorkflowQueryService>().PaymentsAsync(2,null,null,1,20));
    }
    [Fact] public async Task Attention_counts_all_sections_before_paging_and_excludes_another_team()
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync(); await f.SessionAsync(2,2);
        f.Db.AddRange(new LoadRequest{Id=1,LoadRequestNumber="LR1",SalesRepId=1,WarehouseId=1},new LoadRequest{Id=2,LoadRequestNumber="LR2",SalesRepId=2,WarehouseId=2});
        f.Db.Add(new Invoice{Id=1,InvoiceNumber="INV",CustomerId=1,WarehouseId=1,SalesRepId=1,TotalAmount=100});
        f.Db.Add(new InvoiceReturn{ReturnNumber="RET1",InvoiceId=1,CustomerId=1,SalesRepId=1,SalesRepSessionId=1,Status=ReturnStatus.Pending});
        f.Db.Add(new SalesRepDayClosing{ClosingNumber="DC1",SalesRepId=1,SalesRepSessionId=1,WarehouseId=1,Status=SalesRepDayClosingStatus.Submitted});
        var date=new Salesync.Application.Services.BusinessClock(TimeProvider.System,"Africa/Cairo").Today;
        f.Db.Add(new Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit{SalesRepId=1,CustomerId=1,SalesRepSessionId=1,Status=VisitStatus.InProgress,VisitDate=DateTime.UtcNow});
        await f.Db.SaveChangesAsync(); f.Actor.Role="Supervisor"; f.Actor.UserId="supervisor";
        var data=await f.Get<MobileWorkflowQueryService>().AttentionAsync(date,null,2,2);
        Assert.Equal(4,data.TotalCount); Assert.Equal(2,data.TotalPages); Assert.Equal(2,data.Items.Count);
        Assert.All(data.Items,x=>Assert.Equal(1,x.SalesRepId));
        Assert.All(new[]{"load","return","closing","visit"},kind=>Assert.Equal(1,data.Counts[kind]));
        var returns=await f.Get<MobileWorkflowQueryService>().AttentionAsync(date,"return",1,20);
        Assert.Single(returns.Items); Assert.Equal("return",returns.Items.Single().Kind); Assert.Equal(1,returns.TotalCount);
    }
    [Theory]
    [InlineData("invoice")][InlineData("return")][InlineData("load")][InlineData("unload")][InlineData("visit")][InlineData("closing")]
    public async Task Public_document_read_and_cancel_cannot_escape_rep_scope(string kind)
    {
        await using var f=await WorkflowFixture.CreateAsync(); await f.SessionAsync(2,2);
        f.Db.Add(new Invoice{Id=2,InvoiceNumber="INV2",CustomerId=1,WarehouseId=2,SalesRepId=2});
        f.Db.Add(new InvoiceReturn{Id=2,ReturnNumber="RET2",InvoiceId=2,CustomerId=1,SalesRepId=2});
        f.Db.Add(new LoadRequest{Id=2,LoadRequestNumber="LR2",SalesRepId=2,WarehouseId=2});
        f.Db.Add(new SalesRepUnloadRequest{Id=2,RequestNumber="UN2",SalesRepId=2,SalesRepSessionId=2,WarehouseId=2});
        f.Db.Add(new SalesRepDayClosing{Id=2,ClosingNumber="DC2",SalesRepId=2,SalesRepSessionId=2,WarehouseId=2,Status=SalesRepDayClosingStatus.Submitted});
        f.Db.Add(new Salesync.Domain.Modules.CustomerVisit.Entities.CustomerVisit{Id=2,SalesRepId=2,CustomerId=1,SalesRepSessionId=2,Status=VisitStatus.InProgress});
        await f.Db.SaveChangesAsync();
        Func<Task> read=kind switch {
            "invoice" => async()=>{await f.Get<IInvoiceService>().GetByIdAsync(2);},
            "return" => async()=>{await f.Get<IInvoiceReturnService>().GetByIdAsync(2);},
            "load" => async()=>{await f.Get<ILoadRequestService>().GetByIdAsync(2);},
            "unload" => async()=>{await f.Get<ISalesRepUnloadRequestService>().GetByIdAsync(2);},
            "closing" => async()=>{await f.Get<ISalesRepDayClosingService>().GetByIdAsync(2);},
            _ => async()=>{await f.Get<ICustomerVisitService>().GetByIdAsync(2);}
        };
        await Assert.ThrowsAsync<KeyNotFoundException>(read);
        Func<Task> cancel=kind switch {
            "invoice" => ()=>f.Get<IInvoiceService>().CancelAsync(2),
            "return" => async()=>{await f.Get<IInvoiceReturnService>().CancelAsync(2);},
            "load" => ()=>f.Get<ILoadRequestService>().CancelAsync(2),
            "unload" => ()=>f.Get<ISalesRepUnloadRequestService>().CancelAsync(2,null),
            "closing" => ()=>f.Get<ISalesRepDayClosingService>().CancelAsync(2),
            _ => ()=>f.Get<ICustomerVisitService>().CancelAsync(2)
        };
        var error=await Record.ExceptionAsync(cancel);
        Assert.True(error is ForbiddenException or KeyNotFoundException,$"Unexpected exception: {error}");
        Assert.Equal(1,await f.Db.Invoices.CountAsync());
    }
    [Fact] public async Task General_invoice_update_cannot_bypass_confirmation()
    {
        await using var f=await WorkflowFixture.CreateAsync();
        f.Db.Add(new Invoice{Id=1,InvoiceNumber="DRAFT",CustomerId=1,WarehouseId=1,SalesRepId=1}); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>f.Get<IInvoiceService>().UpdateAsync(1,new UpdateInvoiceDto{Status=InvoiceStatus.Confirmed}));
        Assert.Equal(InvoiceStatus.Draft,(await f.Db.Invoices.SingleAsync()).Status);
    }
    [Fact] public async Task Treasury_receivables_and_cash_boxes_are_branch_scoped()
    {
        await using var f=await WorkflowFixture.CreateAsync(); f.Actor.Role="Treasury"; f.Actor.UserId="treasury";
        f.Db.AddRange(new Salesync.Domain.Modules.Treasury.Entities.CashBox{Id=1,Code="ONE",Name="One",BranchId=1},
            new Salesync.Domain.Modules.Treasury.Entities.CashBox{Id=2,Code="TWO",Name="Two",BranchId=2});
        f.Db.AddRange(new Salesync.Domain.Modules.Treasury.Entities.SalesRepCashLedger{SalesRepId=1,Amount=-10},
            new Salesync.Domain.Modules.Treasury.Entities.SalesRepCashLedger{SalesRepId=2,Amount=-90});
        await f.Db.SaveChangesAsync();
        var treasury=f.Get<ITreasuryService>(); var boxes=await treasury.GetCashBoxesAsync(); Assert.Equal(1,boxes.Single().Id);
        await Assert.ThrowsAsync<KeyNotFoundException>(()=>treasury.GetCashBoxByIdAsync(2));
        await Assert.ThrowsAsync<ForbiddenException>(()=>treasury.GetSalesRepLedgerAsync(2));
        var receivables=f.Get<IReceivablesService>();
        Assert.Equal(1,(await receivables.GetSalesRepReceivablesAsync()).Single().SalesRepId);
        await Assert.ThrowsAsync<ForbiddenException>(()=>receivables.PaySalesRepDebtAsync(2,new(){CashBoxId=1,Amount=10}));
        Assert.Empty(await f.Db.TreasuryTransactions.ToListAsync());
    }

}
