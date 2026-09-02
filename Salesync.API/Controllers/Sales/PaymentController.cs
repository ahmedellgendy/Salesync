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

        [HttpGet]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> GetAllAsync()
        {
            var result =
                await _paymentService.GetAllAsync();

            return Ok(
                ApiResponse<IEnumerable<PaymentDto>>
                    .SuccessResponse(result));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var result =
                await _paymentService.GetByIdAsync(id);

            return Ok(
                ApiResponse<PaymentDto>
                    .SuccessResponse(result));
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
