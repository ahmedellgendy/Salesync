using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Salesync.API.Filters;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Dtos;
using Salesync.Application.Modules.Treasury.Dtos.Receivables;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Domain.Common.Enums.Treasury;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Domain.Modules.Treasury.Entities;
using Xunit;

namespace Salesync.Tests;

public class TreasuryReplayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_treasury_command_replayed_with_same_key_posts_one_movement_only(bool debtPayment)
    {
        await using var f = await WorkflowFixture.CreateAsync(); f.Actor.Role = "Admin";
        f.Db.Add(new CashBox { Id = 1, Code = "C1", Name = "Cash", BranchId = 1, CurrentBalance = 100 });
        if (debtPayment) f.Db.Add(new SalesRepCashLedger { SalesRepId = 1, Amount = -100, BalanceAfter = -100, Source = SalesRepCashLedgerSource.ManualAdjustment });
        await f.Db.SaveChangesAsync();
        var key = Guid.NewGuid().ToString();
        object dto = debtPayment ? new PaySalesRepDebtDto { CashBoxId = 1, Amount = 20 }
            : new CreateTreasuryCashOutDto { CashBoxId = 1, Amount = 20, Source = TreasuryTransactionSource.Adjustment };
        ActionExecutingContext Context() {
            var http = new DefaultHttpContext(); http.Request.Method = "POST";
            http.Request.Path = debtPayment ? "/api/treasury/receivables/sales-reps/1/debt-payment" : "/api/Treasury/cash-out";
            http.Request.Headers["Idempotency-Key"] = key;
            return new(new ActionContext(http, new RouteData(), new ActionDescriptor()), [], new Dictionary<string, object?> { ["dto"] = dto }, new object());
        }
        var calls = 0;
        async Task<ActionExecutedContext> Execute(ActionExecutingContext context) {
            calls++;
            object data = debtPayment ? await f.Get<IReceivablesService>().PaySalesRepDebtAsync(1, (PaySalesRepDebtDto)dto)
                : await f.Get<ITreasuryService>().CreateCashOutAsync((CreateTreasuryCashOutDto)dto);
            return new(context, [], context.Controller) { Result = new OkObjectResult(new { success = true, data }) };
        }
        var filter = new FinancialOperationFilter(f.Unit, f.Db, f.Actor, f.Get<ISalesAccessScopeService>());
        var first = Context(); await filter.OnActionExecutionAsync(first, () => Execute(first));
        var retry = Context(); await filter.OnActionExecutionAsync(retry, () => Execute(retry));
        Assert.Equal(1, calls);
        Assert.Equal(debtPayment ? 120 : 80, (await f.Db.CashBoxes.AsNoTracking().SingleAsync()).CurrentBalance);
        Assert.Single(await f.Db.TreasuryTransactions.ToListAsync());
        Assert.Single(await f.Db.Set<OperationExecution>().ToListAsync());
        Assert.Equal("true", retry.HttpContext.Response.Headers["Idempotency-Replayed"]);
        if (debtPayment) Assert.Equal(-80, await f.Db.SalesRepCashLedgers.SumAsync(x => x.Amount));
    }
}
