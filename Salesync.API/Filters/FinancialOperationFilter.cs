using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Services;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.API.Filters;

/// Optional for legacy clients. New mobile clients keep one UUID per command.
/// Authorization runs before this action filter, including on cached responses.
public sealed class FinancialOperationFilter(
    IUnitOfWork unit, SalesyncDbContext db, ICurrentUserService actor,
    ISalesAccessScopeService access) : IAsyncActionFilter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        var path = request.Path.Value?.ToLowerInvariant() ?? "";
        var financial = new[] { "/api/mobile/salesrep/", "/api/invoice", "/api/invoicereturn", "/api/payment", "/api/loadrequest",
            "/api/unload-requests", "/api/salesrepsession", "/api/salesrepdayclosing", "/api/treasury/" }
            .Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
        if (!financial || HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) ||
            !request.Headers.TryGetValue("Idempotency-Key", out var rawKey))
        { await next(); return; }
        if (rawKey.Count != 1 || !Guid.TryParse(rawKey[0], out var parsed))
            throw new ArgumentException("IDEMPOTENCY_KEY_INVALID: Use one UUID for the original command.");
        if (string.IsNullOrWhiteSpace(actor.UserId)) throw new UnauthorizedAccessException("Authentication required.");
        await access.GetAsync();
        var key = parsed.ToString("D");
        var operation = request.Method.ToUpperInvariant() + " " + path;
        var payload = JsonSerializer.Serialize(context.ActionArguments, Json);
        var hash = OperationFingerprint.Compute(operation, payload);
        context.Result = await unit.ExecuteInTransactionAsync<IActionResult>(async () =>
        {
            var saved = await db.Set<OperationExecution>().AsNoTracking()
                .SingleOrDefaultAsync(x => x.ActorId == actor.UserId && x.OperationKey == key);
            if (saved != null)
            {
                if (saved.RequestHash != hash || saved.Operation != operation)
                    throw new InvalidOperationException("IDEMPOTENCY_CONFLICT: This key was used with different command data.");
                if (saved.SalesRepId.HasValue) await access.RequireSalesRepAsync(saved.SalesRepId);
                context.HttpContext.Response.Headers["Idempotency-Replayed"] = "true";
                return new ContentResult { Content = saved.ResponseJson, StatusCode = saved.ResponseStatusCode, ContentType = "application/json; charset=utf-8" };
            }
            var executed = await next();
            if (executed.Exception != null && !executed.ExceptionHandled)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(executed.Exception).Throw();
            if (executed.Result is not ObjectResult result || (result.StatusCode ?? 200) >= 400)
                throw new InvalidOperationException("OPERATION_NOT_COMPLETED: No successful operation receipt was produced.");
            var response = JsonSerializer.Serialize(result.Value, Json);
            using var document = JsonDocument.Parse(response);
            if (document.RootElement.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False)
                throw new InvalidOperationException("OPERATION_NOT_COMPLETED: The operation was rejected.");
            var data = document.RootElement.TryGetProperty("data", out var envelope) ? envelope : document.RootElement;
            int? id = Number(data, "id");
            int? rep = Number(data, "salesRepId");
            if (rep.HasValue) await access.RequireSalesRepAsync(rep);
            db.Set<OperationExecution>().Add(new OperationExecution {
                ActorId = actor.UserId!, OperationKey = key, RequestHash = hash, Operation = operation,
                ResourceId = id, SalesRepId = rep, ResponseJson = response,
                ResponseStatusCode = result.StatusCode ?? 200, CompletedAt = DateTime.UtcNow });
            await unit.CompleteAsync();
            return result;
        });
    }

    private static int? Number(JsonElement data, string key) => data.ValueKind == JsonValueKind.Object &&
        data.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;
}
