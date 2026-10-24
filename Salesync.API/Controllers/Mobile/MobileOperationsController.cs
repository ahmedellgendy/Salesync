using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Salesync.API.Responses;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Interfaces.Services;
using Salesync.Domain.Modules.Sales.Entities;
using Salesync.Infrastructure.Data;

namespace Salesync.API.Controllers.Mobile;

[ApiController, Route("api/mobile/operations"), Authorize(Roles = "SalesRep,Supervisor,Admin,Treasury,Warehouse")]
public sealed class MobileOperationsController(SalesyncDbContext db, ICurrentUserService actor, ISalesAccessScopeService access) : ControllerBase
{
    [HttpGet("{operationKey:guid}")]
    public async Task<ActionResult<ApiResponse<MobileOperationReceipt>>> Get(Guid operationKey, CancellationToken cancellationToken)
    {
        await access.GetAsync();
        var key = operationKey.ToString("D");
        var receipt = await db.Set<OperationExecution>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.ActorId == actor.UserId && x.OperationKey == key, cancellationToken);
        if (receipt == null) return NotFound(ApiResponse<MobileOperationReceipt>.NotFoundResponse(
            "OPERATION_NOT_OBSERVED: The command may still be in flight. Do not create a new operation key."));
        if (receipt.SalesRepId.HasValue) await access.RequireSalesRepAsync(receipt.SalesRepId);
        return Ok(ApiResponse<MobileOperationReceipt>.SuccessResponse(new(receipt.OperationKey, "Succeeded",
            receipt.ResourceId, receipt.Operation, receipt.CompletedAt)));
    }
}
