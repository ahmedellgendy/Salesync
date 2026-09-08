using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Extensions;
using Salesync.Application.Common.Licensing;
using Salesync.Application.Modules.Identity.Dtos.User;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Modules.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILicenseService _licenseService;


        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILicenseService licenseService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _licenseService = licenseService;
        }


        // =========================================================
        // Get All Users
        // =========================================================

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users =
                await _userManager.Users
                    .OrderByDescending(u => u.IsActive)
                    .ThenBy(u => u.FullName)
                    .ToListAsync();


            var userDtos =
                new List<UserDto>();


            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);


                userDtos.Add(
                    MapToUserDto(
                        user,
                        roles.FirstOrDefault()
                        ?? string.Empty));
            }


            return userDtos;
        }


        // =========================================================
        // Get User By Id
        // =========================================================

        public async Task<UserDto> GetUserByIdAsync(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"User with ID '{id}' not found.");


            var roles =
                await _userManager.GetRolesAsync(user);


            return MapToUserDto(
                user,
                roles.FirstOrDefault()
                ?? string.Empty);
        }


        // =========================================================
        // Create User
        // =========================================================

        public async Task<UserDto> CreateUserAsync(
            CreateUserDto createUserDto)
        {
            // License check before creating a new active user
            await EnsureCanAddActiveUserAsync();


            var existingUser =
                await _userManager.FindByNameAsync(
                    createUserDto.UserName);


            if (existingUser is not null)
            {
                throw new InvalidOperationException(
                    $"User with username '{createUserDto.UserName}' already exists.");
            }


            var existingEmail =
                await _userManager.FindByEmailAsync(
                    createUserDto.Email);


            if (existingEmail is not null)
            {
                throw new InvalidOperationException(
                    $"Email '{createUserDto.Email}' already exists.");
            }


            if (!await _roleManager.RoleExistsAsync(
                    createUserDto.Role))
            {
                throw new KeyNotFoundException(
                    $"Role '{createUserDto.Role}' does not exist.");
            }


            var user =
                new ApplicationUser
                {
                    UserName =
                        createUserDto.UserName,

                    Email =
                        createUserDto.Email,

                    FullName =
                        createUserDto.FullName,

                    BranchId =
                        createUserDto.BranchId,

                    BusinessUnitId =
                        createUserDto.BusinessUnitId,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };


            var result =
                await _userManager.CreateAsync(
                    user,
                    createUserDto.Password);


            result.EnsureSuccess();


            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    createUserDto.Role);


            if (!roleResult.Succeeded)
            {
                // Prevent leaving a user without a role
                await _userManager.DeleteAsync(user);

                roleResult.EnsureSuccess();
            }


            return MapToUserDto(
                user,
                createUserDto.Role);
        }


        // =========================================================
        // Create Sales Rep User
        // =========================================================

        public async Task<UserDto> CreateSalesRepUserAsync(
            CreateSalesRepUserDto createSalesRepUserDto)
        {
            const string salesRepRole =
                "SalesRep";


            // SalesRep user is also an active application user,
            // so it consumes one licensed user seat.
            await EnsureCanAddActiveUserAsync();


            var userName =
                createSalesRepUserDto.UserName.Trim();


            var email =
                createSalesRepUserDto.Email?.Trim();


            var existingUser =
                await _userManager.FindByNameAsync(
                    userName);


            if (existingUser is not null)
            {
                throw new InvalidOperationException(
                    $"User with username '{userName}' already exists.");
            }


            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingEmail =
                    await _userManager.FindByEmailAsync(
                        email);


                if (existingEmail is not null)
                {
                    throw new InvalidOperationException(
                        $"Email '{email}' already exists.");
                }
            }


            if (!await _roleManager.RoleExistsAsync(
                    salesRepRole))
            {
                throw new KeyNotFoundException(
                    $"Role '{salesRepRole}' does not exist.");
            }


            var user =
                new ApplicationUser
                {
                    UserName =
                        userName,

                    Email =
                        email,

                    PhoneNumber =
                        createSalesRepUserDto.PhoneNumber.Trim(),

                    FullName =
                        createSalesRepUserDto.FullName.Trim(),

                    BranchId =
                        createSalesRepUserDto.BranchId,

                    BusinessUnitId =
                        createSalesRepUserDto.BusinessUnitId,

                    IsActive =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };


            var createResult =
                await _userManager.CreateAsync(
                    user,
                    createSalesRepUserDto.Password);


            createResult.EnsureSuccess();


            var roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    salesRepRole);


            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);

                roleResult.EnsureSuccess();
            }


            return MapToUserDto(
                user,
                salesRepRole);
        }


        // =========================================================
        // Update User
        // =========================================================

        public async Task<UserDto> UpdateUserAsync(
            string id,
            UpdateUserDto updateUserDto)
        {
            var user =
                await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"User with ID '{id}' not found.");


            // Protect against reactivation through UpdateUserAsync
            if (updateUserDto.IsActive == true &&
                !user.IsActive)
            {
                await EnsureCanAddActiveUserAsync();
            }


            UpdateUserFromDto(
                user,
                updateUserDto);


            if (updateUserDto.Role is not null)
            {
                await UpdateUserRoleAsync(
                    user,
                    updateUserDto.Role);
            }


            var result =
                await _userManager.UpdateAsync(user);


            result.EnsureSuccess();


            var roles =
                await _userManager.GetRolesAsync(user);


            return MapToUserDto(
                user,
                roles.FirstOrDefault()
                ?? string.Empty);
        }


        // =========================================================
        // Activate / Deactivate User
        // =========================================================

        public async Task<UserDto> SetUserActiveStatusAsync(
            string id,
            bool isActive)
        {
            var user =
                await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"User with ID '{id}' not found.");


            // Reactivation consumes a licensed active-user seat.
            if (isActive &&
                !user.IsActive)
            {
                await EnsureCanAddActiveUserAsync();
            }


            user.IsActive =
                isActive;


            var result =
                await _userManager.UpdateAsync(user);


            result.EnsureSuccess();


            var roles =
                await _userManager.GetRolesAsync(user);


            return MapToUserDto(
                user,
                roles.FirstOrDefault()
                ?? string.Empty);
        }


        // =========================================================
        // Reset Password
        // =========================================================

        public async Task ResetUserPasswordAsync(
            string id,
            string newPassword)
        {
            var user =
                await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"User with ID '{id}' not found.");


            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(user);


            var result =
                await _userManager
                    .ResetPasswordAsync(
                        user,
                        token,
                        newPassword);


            result.EnsureSuccess();
        }


        // =========================================================
        // Delete / Deactivate User
        // =========================================================

        public async Task DeleteUserAsync(
            string id)
        {
            var user =
                await _userManager.FindByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"User with ID '{id}' not found.");


            user.IsActive =
                false;


            var result =
                await _userManager.UpdateAsync(user);


            result.EnsureSuccess();
        }


        #region Private Helper Methods


        // =========================================================
        // License User Limit
        // =========================================================

        private async Task EnsureCanAddActiveUserAsync()
        {
            var currentActiveUsers =
                await _userManager.Users
                    .CountAsync(x =>
                        x.IsActive);


            _licenseService
                .EnsureCanAddActiveUser(
                    currentActiveUsers);
        }


        // =========================================================
        // Mapping
        // =========================================================

        private static UserDto MapToUserDto(
            ApplicationUser user,
            string role)
        {
            return new UserDto
            {
                Id =
                    user.Id,

                FullName =
                    user.FullName,

                UserName =
                    user.UserName!,

                Email =
                    user.Email!,

                Role =
                    role,

                BranchId =
                    user.BranchId,

                BusinessUnitId =
                    user.BusinessUnitId,

                IsActive =
                    user.IsActive,

                CreatedAt =
                    user.CreatedAt,

                LastLoginAt =
                    user.LastLoginAt
            };
        }


        // =========================================================
        // Update User Fields
        // =========================================================

        private static void UpdateUserFromDto(
            ApplicationUser user,
            UpdateUserDto dto)
        {
            if (dto.FullName is not null)
            {
                user.FullName =
                    dto.FullName;
            }


            if (dto.ClearBranch)
            {
                user.BranchId =
                    null;
            }
            else if (dto.BranchId.HasValue)
            {
                user.BranchId =
                    dto.BranchId.Value;
            }


            if (dto.BusinessUnitId is not null)
            {
                user.BusinessUnitId =
                    dto.BusinessUnitId;
            }


            if (dto.IsActive.HasValue)
            {
                user.IsActive =
                    dto.IsActive.Value;
            }
        }


        // =========================================================
        // Update Role
        // =========================================================

        private async Task UpdateUserRoleAsync(
            ApplicationUser user,
            string newRole)
        {
            if (!await _roleManager.RoleExistsAsync(
                    newRole))
            {
                throw new KeyNotFoundException(
                    $"Role '{newRole}' not found.");
            }


            var currentRoles =
                await _userManager
                    .GetRolesAsync(user);


            if (currentRoles.Count > 0)
            {
                var removeResult =
                    await _userManager
                        .RemoveFromRolesAsync(
                            user,
                            currentRoles);


                removeResult.EnsureSuccess();
            }


            var addResult =
                await _userManager
                    .AddToRoleAsync(
                        user,
                        newRole);


            addResult.EnsureSuccess();
        }


        #endregion
    }
}