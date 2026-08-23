namespace Salesync.Application.Modules.Profile.Dtos
{
    public class CurrentUserProfileDto
    {
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string Role { get; set; } = string.Empty;

        public int? BranchId { get; set; }

        public string? BranchName { get; set; }

        public int? BusinessUnitId { get; set; }

        public int? SalesRepId { get; set; }

        public string? SalesRepCode { get; set; }

        public string? ProfileImageUrl { get; set; }
    }
}