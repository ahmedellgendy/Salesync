using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Salesync.API.Filters;
using Salesync.Application.Interfaces.Services;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Xunit;

namespace Salesync.Tests;

public class OperationTests
{
    private static ActionExecutingContext Context(string key, decimal amount=10)
    {
        var http=new DefaultHttpContext(); http.Request.Method="POST"; http.Request.Path="/api/Payment";
        http.Request.Headers["Idempotency-Key"]=key;
        return new(new ActionContext(http,new RouteData(),new ActionDescriptor()),[], new Dictionary<string,object?>{{"dto",new {amount}}},new object());
    }
    private static FinancialOperationFilter Filter(WorkflowFixture f)=>new(f.Unit,f.Db,f.Actor,f.Get<ISalesAccessScopeService>());
    [Fact] public async Task Same_key_replays_saved_result_without_running_command_and_changed_payload_conflicts()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var filter=Filter(f); var key=Guid.NewGuid().ToString(); var calls=0;
        async Task<ActionExecutedContext> Execute(ActionExecutingContext c) {
            calls++; f.Db.Add(new CashBox{Code="ONE",Name="One",BranchId=1}); await f.Db.SaveChangesAsync();
            return new(c,[],c.Controller){Result=new OkObjectResult(new{success=true,data=new{id=42,salesRepId=1}})};
        }
        var first=Context(key); await filter.OnActionExecutionAsync(first,()=>Execute(first));
        var second=Context(key); await filter.OnActionExecutionAsync(second,()=>Execute(second));
        Assert.IsType<ContentResult>(second.Result); Assert.Equal("true",second.HttpContext.Response.Headers["Idempotency-Replayed"]);
        Assert.Equal(1,calls); Assert.Single(await f.Db.Set<OperationExecution>().ToListAsync()); Assert.Single(await f.Db.CashBoxes.ToListAsync());
        var changed=Context(key,20);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>filter.OnActionExecutionAsync(changed,()=>Execute(changed)));
        Assert.Equal(1,calls);
    }
    [Fact] public async Task Failure_rolls_back_command_and_does_not_leave_success_receipt()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var c=Context(Guid.NewGuid().ToString());
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Filter(f).OnActionExecutionAsync(c,async()=>{
            f.Db.Add(new CashBox{Code="FAILED",Name="Failed",BranchId=1}); await f.Db.SaveChangesAsync();
            return new ActionExecutedContext(c,[],c.Controller){Exception=new InvalidOperationException("Injected failure")};
        }));
        Assert.Empty(await f.Db.CashBoxes.ToListAsync()); Assert.Empty(await f.Db.Set<OperationExecution>().ToListAsync());
    }
    [Fact] public async Task Malformed_key_rejected_before_execution()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var c=Context("bad"); var called=false;
        await Assert.ThrowsAsync<ArgumentException>(()=>Filter(f).OnActionExecutionAsync(c,()=>{called=true;return Task.FromResult(new ActionExecutedContext(c,[],c.Controller));}));
        Assert.False(called);
    }
    [Fact] public async Task Same_key_is_isolated_by_actor()
    {
        await using var f=await WorkflowFixture.CreateAsync(); var key=Guid.NewGuid().ToString(); var c=Context(key);
        await Filter(f).OnActionExecutionAsync(c,()=>Task.FromResult(new ActionExecutedContext(c,[],c.Controller){Result=new OkObjectResult(new{success=true,data=new{id=1,salesRepId=1}})}));
        f.Actor.UserId="rep-2";
        var other=Context(key);
        var executed=false;
        await new FinancialOperationFilter(f.Unit,f.Db,f.Actor,new Salesync.Application.Services.SalesAccessScopeService(f.Unit,f.Actor)).OnActionExecutionAsync(other,()=>{
            executed=true; return Task.FromResult(new ActionExecutedContext(other,[],other.Controller){Result=new OkObjectResult(new{success=true,data=new{id=2,salesRepId=2}})});
        });
        Assert.True(executed); Assert.Equal(2,await f.Db.Set<OperationExecution>().CountAsync());
    }

    [Fact] public async Task Cached_receipt_is_reauthorized_when_supervisor_team_changes()
    {
        await using var f=await WorkflowFixture.CreateAsync(); f.Actor.UserId="supervisor"; f.Actor.Role="Supervisor";
        var key=Guid.NewGuid().ToString(); var first=Context(key);
        await Filter(f).OnActionExecutionAsync(first,()=>Task.FromResult(new ActionExecutedContext(first,[],first.Controller){Result=new OkObjectResult(new{success=true,data=new{id=1,salesRepId=1}})}));
        var rep=await f.Db.SalesReps.SingleAsync(x=>x.Id==1); rep.SupervisorId=null; await f.Db.SaveChangesAsync();
        var next=Context(key); var ran=false;
        var filter=new FinancialOperationFilter(f.Unit,f.Db,f.Actor,new Salesync.Application.Services.SalesAccessScopeService(f.Unit,f.Actor));
        await Assert.ThrowsAsync<Salesync.Application.Common.Exceptions.ForbiddenException>(()=>filter.OnActionExecutionAsync(next,()=>{ran=true;return Task.FromResult(new ActionExecutedContext(next,[],next.Controller));}));
        Assert.False(ran); Assert.Single(await f.Db.Set<OperationExecution>().ToListAsync());
    }
    [Theory]
    [InlineData(2026,4,24,23)]
    [InlineData(2026,10,29,25)]
    public void Cairo_day_boundaries_handle_clock_changes(int year,int month,int day,int hours)
    {
        var clock=new Salesync.Application.Services.BusinessClock(TimeProvider.System,"Africa/Cairo");
        var date=new DateTime(year,month,day);
        Assert.Equal(hours,(clock.UtcStart(date.AddDays(1))-clock.UtcStart(date)).TotalHours);
    }
}
