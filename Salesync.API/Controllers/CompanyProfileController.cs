using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.Application.Modules.Company.Dtos;
using Salesync.Application.Modules.Company.Interfaces;

namespace Salesync.API.Controllers;

[ApiController]
[Route("api/company")]
public class CompanyProfileController : ControllerBase
{
    private readonly ICompanyProfileService _companyProfileService;

    public CompanyProfileController(
        ICompanyProfileService companyProfileService)
    {
        _companyProfileService = companyProfileService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var result =
            await _companyProfileService.GetAsync();

        return Ok(new
        {
            success = true,
            message = "Company profile loaded successfully.",
            data = result
        });
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateCompanyProfileDto dto)
    {
        var result =
            await _companyProfileService.UpdateAsync(dto);

        return Ok(new
        {
            success = true,
            message = "Company profile updated successfully.",
            data = result
        });
    }
}