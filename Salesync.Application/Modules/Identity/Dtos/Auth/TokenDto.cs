namespace Salesync.Application.Modules.Identity.Dtos.Auth
{
    public class TokenDto
    {
        public string Token { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
