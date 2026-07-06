using Microsoft.AspNetCore.Http;
using Salesync.Application.Interfaces.Services;
using System.Security.Claims;

namespace Salesync.Infrastructure.Modules.Identity.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId =>
            _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("UserId")?.Value;

        public string? Role =>
            _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;

        public int? SalesRepId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst("SalesRepId")?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public int? BranchId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst("BranchId")?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }

        public int? BusinessUnitId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.User.FindFirst("BusinessUnitId")?.Value;
                return int.TryParse(value, out var id) ? id : null;
            }
        }
    }
}