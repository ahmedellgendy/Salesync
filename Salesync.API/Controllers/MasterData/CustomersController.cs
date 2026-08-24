using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Modules.MasterData.Interfaces.Services;

namespace Salesync.API.Controllers.MasterData
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(
            ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers =
                await _customerService.GetAllAsync();

            return Ok(
                ApiResponse<IEnumerable<CustomerDto>>
                    .SuccessResponse(
                        customers,
                        "Customers retrieved successfully",
                        200));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var customer =
                await _customerService.GetByIdAsync(id);

            if (customer == null)
            {
                return NotFound(
                    ApiResponse<CustomerDto>
                        .NotFoundResponse(
                            $"Customer with ID {id} not found"));
            }

            return Ok(
                ApiResponse<CustomerDto>
                    .SuccessResponse(
                        customer,
                        "Customer retrieved successfully",
                        200));
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCustomerDto dto)
        {
            var createdCustomer =
                await _customerService.CreateAsync(dto);

            return CreatedAtAction(
                nameof(GetById),
                new { id = createdCustomer.Id },
                ApiResponse<CustomerDto>.SuccessResponse(
                    createdCustomer,
                    "Customer created successfully",
                    201));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCustomerDto dto)
        {
            if (id != dto.Id)
            {
                return BadRequest(
                    ApiResponse<object>.ErrorResponse(
                        "ID in URL does not match ID in body.",
                        400));
            }

            var updatedCustomer =
                await _customerService.UpdateAsync(
                    id,
                    dto);

            return Ok(
                ApiResponse<CustomerDto>.SuccessResponse(
                    updatedCustomer,
                    "Customer updated successfully",
                    200));
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _customerService.DeleteAsync(id);

            return Ok(
                ApiResponse<object>.SuccessResponse(
                    null,
                    $"Customer with ID {id} deactivated successfully."));
        }
    }
}