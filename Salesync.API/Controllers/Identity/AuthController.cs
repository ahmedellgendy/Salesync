using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Identity.Dtos.Auth;
using Salesync.Application.Modules.Identity.Interfaces;

namespace Salesync.API.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var token = await _authService.LoginAsync(loginDto);
            return Ok(ApiResponse<TokenDto>.SuccessResponse(token, "Login Successful"));
        }

    }
}
