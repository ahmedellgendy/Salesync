using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Salesync.API.Responses;
using Salesync.Application.Modules.Identity.Dtos.User;
using Salesync.Application.Modules.Identity.Interfaces;

namespace Salesync.API.Controllers.Identity
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet] // GET: api/users
        public async Task<IActionResult> GetAllAsync()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResponse(
                users, "Users retrieved successfully"));
        }

        [HttpGet("{id}")] // GET: api/users/{id}
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            return Ok(ApiResponse<UserDto>.SuccessResponse(
                user, "User retrieved successfully"));
        }

        [HttpPost] // POST: api/users
        public async Task<IActionResult> CreateAsync([FromBody] CreateUserDto createUserDto)
        {
            var user = await _userService.CreateUserAsync(createUserDto);
            return Ok(ApiResponse<UserDto>.SuccessResponse(
                user, "User created successfully"));

        }

        [HttpPut("{id}")] // PUT: api/users/{id}
        public async Task<IActionResult> UpdateAsync(string id, [FromBody] UpdateUserDto updateUserDto)
        {
            var user = await _userService.UpdateUserAsync(id, updateUserDto);
            return Ok(ApiResponse<UserDto>.SuccessResponse(
                user, "User updated successfully"));
        }

        [HttpDelete("{id}")] // DELETE: api/users/{id}
        public async Task<IActionResult> DeleteAsync(string id)
        {
            await _userService.DeleteUserAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(
                null!, "User deleted successfully"));  
        }
    }
}
