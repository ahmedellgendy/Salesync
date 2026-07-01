using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Sales.Dtos.Payment;
using Salesync.Application.Modules.Sales.Interfaces;

namespace Salesync.API.Controllers.Sales
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]

    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }


        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetByInvoiceIdAsync(int invoiceId)
        {
            var result = await _paymentService.GetByInvoiceIdAsync(invoiceId);
            return Ok(ApiResponse<IEnumerable<PaymentDto>>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> CreateAsync([FromBody] CreatePaymentDto dto)
        {
            var result = await _paymentService.CreateAsync(dto);
            return Ok(ApiResponse<PaymentDto>.SuccessResponse(result, "Payment created successfully"));
        }
    }
}
