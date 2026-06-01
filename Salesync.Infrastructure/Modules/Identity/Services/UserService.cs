using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Extensions;
using Salesync.Application.Modules.Identity.Dtos.User;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Modules.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // get all users with their roles
        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive)
                .ToListAsync();

            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles.FirstOrDefault() ?? string.Empty));
            }

            return userDtos;
        }

        // get user by id with role
        public async Task<UserDto> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id)
                 ?? throw new KeyNotFoundException($"User with ID '{id}' not found.");

            var roles = await _userManager.GetRolesAsync(user);

            return MapToUserDto(user, roles.FirstOrDefault() ?? string.Empty);
        }

        // create user with role
        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            //check if user with same username already exists
            var existingUser = await _userManager.FindByNameAsync(createUserDto.UserName);
            if (existingUser != null)
                throw new InvalidOperationException($"User with username '{createUserDto.UserName}' already exists.");

            // check if email exists
            var existingEmail = await _userManager.FindByEmailAsync(createUserDto.Email);
            if (existingEmail != null)
                throw new InvalidOperationException($"Email '{createUserDto.Email}' already exists.");

            // check if role exists
            if (!await _roleManager.RoleExistsAsync(createUserDto.Role))
                throw new KeyNotFoundException($"Role '{createUserDto.Role}' does not exist.");

            // create user
            var user = new ApplicationUser
            {
                UserName = createUserDto.UserName,
                Email = createUserDto.Email,
                FullName = createUserDto.FullName,
                BranchId = createUserDto.BranchId,
                BusinessUnitId = createUserDto.BusinessUnitId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            result.EnsureSuccess();

            // assign role 
            await _userManager.AddToRoleAsync(user, createUserDto.Role);

            return MapToUserDto(user, createUserDto.Role);
        }

        // update user and role
        public async Task<UserDto> UpdateUserAsync(string id, UpdateUserDto updateUserDto)
        {
            // find user by id
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException($"User with ID '{id}' not found.");

            // update user properties
            UpdateUserFromDto(user, updateUserDto);

            // update role 
            if (updateUserDto.Role != null)
                await UpdateUserRoleAsync(user, updateUserDto.Role);

            var result = await _userManager.UpdateAsync(user);
            result.EnsureSuccess();

            var roles = await _userManager.GetRolesAsync(user);

            return MapToUserDto(user, roles.FirstOrDefault() ?? string.Empty);

        }

        // delete user 
        public async Task DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException($"User with ID '{id}' not found.");

            user.IsActive = false;

            var result = await _userManager.UpdateAsync(user);
            result.EnsureSuccess();
        }


        #region Private Helper Methods

        // Helper method to map ApplicationUser to UserDto
        private static UserDto MapToUserDto(ApplicationUser user, string role)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = role,
                BranchId = user.BranchId,
                BusinessUnitId = user.BusinessUnitId,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        // helper method to update user properties from UpdateUserDto
        private void UpdateUserFromDto(ApplicationUser user, UpdateUserDto dto)
        {
            if (dto.FullName != null) user.FullName = dto.FullName;
            if (dto.BranchId != null) user.BranchId = dto.BranchId;
            if (dto.BusinessUnitId != null) user.BusinessUnitId = dto.BusinessUnitId;
            if (dto.IsActive.HasValue) user.IsActive = dto.IsActive.Value;
        }

        // helper method to update user role 
        private async Task UpdateUserRoleAsync(ApplicationUser user, string newRole)
        {
            if (!await _roleManager.RoleExistsAsync(newRole))
                throw new KeyNotFoundException($"Role '{newRole}' not found.");

            var currentRoles = await _userManager.GetRolesAsync(user);   // get current roles of the user
            await _userManager.RemoveFromRolesAsync(user, currentRoles); // remove user from current roles
            await _userManager.AddToRoleAsync(user, newRole);            // add user to new role
        }

        #endregion
    }
}