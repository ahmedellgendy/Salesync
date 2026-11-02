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
        private readonly IInvoiceReturnReceivingService _receiving;

        public InvoiceReturnController(IInvoiceReturnService returnService, IInvoiceReturnReceivingService receiving)
        {
            _returnService = returnService;
            _receiving = receiving;
        }
        [HttpGet("{id:int}/receiving")]
        [Authorize(Roles = "Admin,Warehouse,Supervisor,SalesRep,Treasury")]
        public async Task<IActionResult> Receiving(int id) => Ok(
            ApiResponse<ReturnReceivingDto>.SuccessResponse(await _receiving.GetAsync(id)));

        [HttpGet("{id:int}/receiving/operations/{operationId:guid}")]
        [Authorize(Roles = "Admin,Warehouse,Supervisor,SalesRep,Treasury")]
        public async Task<IActionResult> ReceivingOperation(int id, Guid operationId)
        {
            var result = await _receiving.FindOperationAsync(id, operationId);
            return result is null ? NotFound() : Ok(ApiResponse<ReturnReceiptDto>.SuccessResponse(result));
        }

        [HttpPost("{id:int}/receive")]
        [Authorize(Roles = "Admin,Warehouse")]
        public async Task<IActionResult> Receive(int id, [FromBody] ReceiveInvoiceReturnDto dto) => Ok(
            ApiResponse<ReturnReceiptDto>.SuccessResponse(await _receiving.ReceiveAsync(id, dto)));

        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _returnService.GetAllAsync();

            return Ok(
                ApiResponse<IEnumerable<InvoiceReturnDto>>
                    .SuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Supervisor,Warehouse")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _returnService.GetByIdAsync(id);

            return Ok(
                ApiResponse<InvoiceReturnDto>
                    .SuccessResponse(result));
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

        [HttpPut("{id}/cancel")]
        [Authorize(Roles = "SalesRep")]
        public async Task<IActionResult> CancelAsync(int id)
        {
            var result = await _returnService.CancelAsync(id);

            return Ok(ApiResponse<InvoiceReturnDto>.SuccessResponse(result, "Return cancelled successfully"));
        }
    }
}
