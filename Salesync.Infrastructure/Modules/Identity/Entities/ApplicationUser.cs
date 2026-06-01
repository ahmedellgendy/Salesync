using Microsoft.AspNetCore.Identity;
using Salesync.Domain.Modules.MasterData.Entities;

namespace Salesync.Infrastructure.Modules.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public required string FullName { get; set; }
        public int? BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public Branch? Branch { get; set; }
    }
}