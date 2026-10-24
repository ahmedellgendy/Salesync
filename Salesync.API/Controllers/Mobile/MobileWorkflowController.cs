using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.SalesRep.Dtos.Mobile;
using Salesync.Application.Modules.Sales.Dtos.SalesRepSession;
using Salesync.Application.Modules.SalesRep.Services;

namespace Salesync.API.Controllers.Mobile;

[ApiController, Route("api/mobile/salesrep"), Authorize(Roles = "SalesRep")]
public sealed class MobileWorkflowController(MobileWorkflowQueryService queries) : ControllerBase
{
    [HttpGet("sessions")]
    public async Task<ActionResult<ApiResponse<MobilePage<SalesRepSessionDto>>>> Sessions(DateTime? fromDate, DateTime? toDate, int pageNumber = 1, int pageSize = 20) =>
        Ok(ApiResponse<MobilePage<SalesRepSessionDto>>.SuccessResponse(await queries.SessionsAsync(fromDate, toDate, pageNumber, pageSize)));

    [HttpGet("payments")]
    public async Task<ActionResult<ApiResponse<MobilePaymentPage>>> Payments(int? sessionId, DateTime? fromDate, DateTime? toDate, int pageNumber = 1, int pageSize = 20) =>
        Ok(ApiResponse<MobilePaymentPage>.SuccessResponse(await queries.PaymentsAsync(sessionId, fromDate, toDate, pageNumber, pageSize)));

    [HttpGet("day/{sessionId:int}/review")]
    public async Task<ActionResult<ApiResponse<MobileReviewDto>>> Review(int sessionId, int? warehouseId) =>
        Ok(ApiResponse<MobileReviewDto>.SuccessResponse(await queries.ReviewAsync(sessionId, warehouseId)));
}

[ApiController, Route("api/mobile/supervisor"), Authorize(Roles = "Supervisor")]
public sealed class MobileSupervisorWorkflowController(MobileWorkflowQueryService queries) : ControllerBase
{
    [HttpGet("attention")]
    public async Task<ActionResult<ApiResponse<MobileAttentionPage>>> Attention(DateTime? date, string? kind, int pageNumber = 1, int pageSize = 20) =>
        Ok(ApiResponse<MobileAttentionPage>.SuccessResponse(await queries.AttentionAsync(date, kind, pageNumber, pageSize)));
}
