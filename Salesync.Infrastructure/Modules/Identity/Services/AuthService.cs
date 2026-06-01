using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Salesync.Application.Common.Settings;
using Salesync.Application.Modules.Identity.Dtos.Auth;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Infrastructure.Modules.Identity.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Salesync.Infrastructure.Modules.Identity.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;

        public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtSettings)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings.Value;
        }

        // Login
        public async Task<TokenDto> LoginAsync(LoginDto loginDto)
        {
            // check if user exists
            var user = await _userManager.FindByNameAsync(loginDto.UserName)
                ?? throw new UnauthorizedAccessException("Invalid username or password.");

            // check if password is correct 
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
                throw new UnauthorizedAccessException("Invalid username or password.");

            // check if user is active
            if (!user.IsActive)
                throw new UnauthorizedAccessException("User account is inactive.");

            // get user roles
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            // generate token
            var token = await GenerateTokenAsync(user);

            // update last login time
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // return token 
            return new TokenDto
            {
                Token = token,
                UserName = user.UserName!,
                FullName = user.FullName,
                Role = role,
                BranchId = user.BranchId,
                BusinessUnitId = user.BusinessUnitId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes)
            };
        }

        // Generate JWT token
        private async Task<string> GenerateTokenAsync(ApplicationUser user)
        {
            var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var rolesAsClaims = roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("FullName", user.FullName),
                new Claim("BranchId", user.BranchId?.ToString() ?? string.Empty),
                new Claim("BusinessUnitId", user.BusinessUnitId?.ToString() ?? string.Empty)
            }
            .Union(userClaims)
            .Union(rolesAsClaims);

            var symmetricSecuityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var signingCredentials = new SigningCredentials(symmetricSecuityKey, SecurityAlgorithms.HmacSha256);

            var tokenObj = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: signingCredentials
                );

            return new JwtSecurityTokenHandler().WriteToken(tokenObj);

        }
    }
}
