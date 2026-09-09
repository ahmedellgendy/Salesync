using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Common;
using Salesync.Application.Modules.Treasury.Dtos.Receivables;
using Salesync.Application.Modules.Treasury.Interfaces.Services;

namespace Salesync.API.Controllers.Treasury
{
    [ApiController]
    [Route("api/treasury/receivables")]
    [Authorize(Roles = "Admin,Treasury,Management")]
    public class ReceivablesController : ControllerBase
    {
        private readonly IReceivablesService _receivablesService;

        public ReceivablesController(
            IReceivablesService receivablesService)
        {
            _receivablesService = receivablesService;
        }


        [HttpGet("summary")]
        public async Task<ActionResult<ApiResponse<ReceivablesSummaryDto>>>
            GetSummary()
        {
            var result =
                await _receivablesService
                    .GetSummaryAsync();

            return Ok(
                ApiResponse<ReceivablesSummaryDto>
                    .SuccessResponse(result));
        }


        [HttpGet("customers")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerReceivableDto>>>>
            GetCustomerReceivables()
        {
            var result =
                await _receivablesService
                    .GetCustomerReceivablesAsync();

            return Ok(
                ApiResponse<IEnumerable<CustomerReceivableDto>>
                    .SuccessResponse(result));
        }


        [HttpGet("customers/{customerId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<CustomerReceivableInvoiceDto>>>>
            GetCustomerReceivableDetails(
                int customerId)
        {
            var result =
                await _receivablesService
                    .GetCustomerReceivableDetailsAsync(
                        customerId);

            return Ok(
                ApiResponse<IEnumerable<CustomerReceivableInvoiceDto>>
                    .SuccessResponse(result));
        }


        [HttpGet("sales-reps")]
        public async Task<ActionResult<ApiResponse<IEnumerable<SalesRepReceivableDto>>>> GetSalesRepReceivables()
        {
            var result =
                await _receivablesService
                    .GetSalesRepReceivablesAsync();

            return Ok(
                ApiResponse<IEnumerable<SalesRepReceivableDto>>
                    .SuccessResponse(result));
        }

        [HttpPost("sales-reps/{salesRepId:int}/debt-payment")]
        public async Task<ActionResult<ApiResponse<SalesRepDebtPaymentResultDto>>> PaySalesRepDebt(
        int salesRepId,
        [FromBody] PaySalesRepDebtDto dto)
        {
            var result =
                await _receivablesService
                    .PaySalesRepDebtAsync(
                        salesRepId,
                        dto);

            return Ok(
                ApiResponse<SalesRepDebtPaymentResultDto>
                    .SuccessResponse(result));
        }
    }
}