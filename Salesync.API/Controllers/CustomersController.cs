using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Dtos.CustomerDto;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Validators.Customer;

namespace Salesync.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly IValidator<CreateCustomerDto> _createValidator;
        private readonly IValidator<UpdateCustomerDto> _updateValidator;

        public CustomersController(ICustomerService customerService, IValidator<CreateCustomerDto> createValidator, IValidator<UpdateCustomerDto> updateValidator)

        {
            _customerService = customerService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }


        [HttpGet] // GET: api/customers
        public async Task<IActionResult> GetAll()
        {
            var customers = await _customerService.GetAllAsync();
            return Ok(customers);
        }

        [HttpGet("{id}")] // GET: api/customers/{id}
        public async Task<IActionResult> GetById(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
                return NotFound($"Customer with id '{id}' not found.");

            return Ok(customer);
        }

        [HttpPut("{id}")] // PUT: api/customers/{id}
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto updateCustomerDto)
        {
            // 1.FluentValidation - Validate data format
            var validationResult = await _updateValidator.ValidateAsync(updateCustomerDto);
            if (!validationResult.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Validation failed",
                    Errors = validationResult.Errors.Select(e => e.ErrorMessage)
                });
            }

            // 2.Check the ID in the URL matches the ID in the body
            if (id != updateCustomerDto.Id)
                return BadRequest("ID in URL does not match ID in body.");

            // UPDATE Customer
            var updatedCustomer = await _customerService.UpdateAsync(id, updateCustomerDto);
            return Ok(new
            {
                Message = "Customer updated successfully..",
                Customer = updatedCustomer
            });

        }

        [HttpPost] // POST: api/customers
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto createCustomerDto)
        {
            // Validation
            var validationResult = await _createValidator.ValidateAsync(createCustomerDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);


            var createdCustomer = await _customerService.CreateAsync(createCustomerDto);
            return CreatedAtAction(nameof(GetById), new { id = createdCustomer.Id }, new
            {
                Message = "Customer created successfully..",
                CustomerDto = createdCustomer
            });

        }

        [HttpDelete("{id}")] // DELETE: api/customers/{id}
        public async Task<IActionResult> Delete(int id)
        {

            await _customerService.DeleteAsync(id);
            return Ok($"Customer with id '{id}' deleted successfully.");

        }
    }
}
