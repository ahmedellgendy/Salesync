using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Profile.Dtos;
using Salesync.Application.Modules.Profile.Interfaces;

namespace Salesync.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _profileService;

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
        }

        [HttpGet("me")] // GET api/profile/me
        public async Task<IActionResult> GetMyProfile()
        {
            var result = await _profileService.GetCurrentUserAsync();

            return Ok(
                ApiResponse<CurrentUserProfileDto>.SuccessResponse(
                    result,
                    "Profile retrieved successfully."));
        }
    }
}