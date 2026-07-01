using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Dtos.InvoiceReturn;
using Salesync.Application.Modules.Sales.Interfaces;

namespace Salesync.API.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class InvoiceReturnController : ControllerBase
    {
        private readonly IInvoiceReturnService _returnService;

        public InvoiceReturnController(IInvoiceReturnService returnService)
        {
            _returnService = returnService;
        }

        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetByInvoiceIdAsync(int invoiceId)
        {
            var result = await _returnService.GetByInvoiceIdAsync(invoiceId);
            return Ok(ApiResponse<IEnumerable<InvoiceReturnDto>>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateInvoiceReturnDto dto)
        {
            var result = await _returnService.CreateAsync(dto);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return created successfully"));
        }

        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> ApproveAsync(int id)
        {
            var result = await _returnService.ApproveAsync(id);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return approved successfully"));
        }

        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> RejectAsync(int id)
        {
            var result = await _returnService.RejectAsync(id);
            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return rejected successfully"));
        }
    }
}
