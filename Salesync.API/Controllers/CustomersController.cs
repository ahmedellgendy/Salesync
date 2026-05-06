using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Dtos.CustomerDto;
using Salesync.Application.Interfaces.Services;

namespace Salesync.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        
        public CustomersController(ICustomerService customerService)

        {
            _customerService = customerService;
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

        [HttpPost] // POST: api/customers
        public async Task<IActionResult> Create([FromBody] CreateCustomerDto createCustomerDto)
        {
            var createdCustomer = await _customerService.CreateAsync(createCustomerDto);
            return CreatedAtAction(nameof(GetById), new { id = createdCustomer.Id }, new
            {
                Message = "Customer created successfully..",
                CustomerDto = createdCustomer
            });
        }

        [HttpPut("{id}")] // PUT: api/customers/{id}
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto updateCustomerDto)
        {
            // Check the ID in the URL matches the ID in the body
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

        [HttpDelete("{id}")] // DELETE: api/customers/{id}
        public async Task<IActionResult> Delete(int id)
        {

            await _customerService.DeleteAsync(id);
            return Ok($"Customer with id '{id}' deleted successfully.");

        }
    }
}
