using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Common.Licensing;

namespace Salesync.API.Controllers.Licensing
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;


        public LicenseController(
            ILicenseService licenseService)
        {
            _licenseService =
                licenseService;
        }


        [HttpGet("status")]
        public async Task<IActionResult> GetStatusAsync()
        {
            var status =
                await _licenseService
                    .GetDetailedStatusAsync();


            return Ok(
                ApiResponse<LicenseStatusDto>
                    .SuccessResponse(
                        status,
                        "License status retrieved successfully"));
        }
    }
}