using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.CustomerVisit.Dtos;
using Salesync.Application.Modules.CustomerVisit.Interfaces;

namespace Salesync.API.Controllers.CustomerVisit
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerVisitController : Controller
    {
        private readonly ICustomerVisitService _customerVisitService;

        public CustomerVisitController(ICustomerVisitService customerVisitService)
        {
            _customerVisitService = customerVisitService;
        }

        [HttpGet] // GET: api/CustomerVisit
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> GetAll([FromQuery] CustomerVisitFilterDto filter)
        {
            var result = await _customerVisitService.GetAllAsync(filter);
            return Ok(ApiResponse<IEnumerable<CustomerVisitDto>>.SuccessResponse(result, "Customer visits retrieved successfully."));
        }

        [HttpGet("{id}")]   // GET: api/CustomerVisit/5
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _customerVisitService.GetByIdAsync(id);
            return Ok(ApiResponse<CustomerVisitDto>.SuccessResponse(result, "Customer visit retrieved successfully."));
        }

        [HttpGet("salesrep/{salesRepId}")] // GET: api/CustomerVisit/salesrep/5
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> GetBySalesRep(int salesRepId)
        {
            var result = await _customerVisitService.GetBySalesRepAsync(salesRepId);
            return Ok(ApiResponse<IEnumerable<CustomerVisitDto>>.SuccessResponse(result, "Sales rep visits retrieved successfully."));
        }

        [HttpGet("customer/{customerId}")] // GET: api/CustomerVisit/customer/5
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> GetByCustomer(int customerId)
        {
            var result = await _customerVisitService.GetByCustomerAsync(customerId);
            return Ok(ApiResponse<IEnumerable<CustomerVisitDto>>.SuccessResponse(result, "Customer visit history retrieved successfully."));
        }

        [HttpPost] // POST: api/CustomerVisit
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> Create([FromBody] CreateCustomerVisitDto dto)
        {
            var result = await _customerVisitService.CreateAsync(dto);
            return Ok(ApiResponse<CustomerVisitDto>.SuccessResponse(result, "Customer visit created successfully."));
        }

        [HttpPut("{id}/cancel")] // PUT: api/CustomerVisit/5/cancel
        [Authorize(Roles = "Admin,Supervisor,SalesRep")]
        public async Task<IActionResult> Cancel(int id)
        {
            await _customerVisitService.CancelAsync(id);
            return Ok(ApiResponse<string>.SuccessResponse("Cancelled", "Customer visit cancelled successfully."));
        }
    }
}