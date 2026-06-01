using Microsoft.AspNetCore.Identity;

namespace Salesync.Infrastructure.Modules.Identity.Entities
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
