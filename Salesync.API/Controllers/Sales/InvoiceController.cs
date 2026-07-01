using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Dtos.Invoice;
using Salesync.Application.Modules.Sales.Interfaces;

namespace Salesync.API.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet] // GET: api/invoice
        public async Task<IActionResult> GetAllAsync()
        {
            var result = await _invoiceService.GetAllAsync();
            return Ok(ApiResponse<IEnumerable<InvoiceDto>>.SuccessResponse(result));
        }

        [HttpGet("{id}")] // GET: api/invoice/id
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result = await _invoiceService.GetByIdAsync(id);
            return Ok(ApiResponse<InvoiceDto>.SuccessResponse(result));
        }

        [HttpPost] // POST: api/invoice
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CreateAsync([FromBody] CreateInvoiceDto dto)
        {
            var result = await _invoiceService.CreateAsync(dto);
            return Ok(ApiResponse<InvoiceDto>.SuccessResponse(result, "Invoice created successfully"));
        }

        [HttpPut("{id}")] // PUT: api/invoice/id
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> UpdateAsync(int id, [FromBody] UpdateInvoiceDto dto)
        {
            var result = await _invoiceService.UpdateAsync(id, dto);
            return Ok(ApiResponse<InvoiceDto>.SuccessResponse(result, "Invoice updated successfully"));
        }

        [HttpPut("{id}/cancel")] // PUT: api/invoice/id/cancel
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> CancelAsync(int id)
        {
            await _invoiceService.CancelAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Invoice cancelled successfully"));
        }
    }
}
