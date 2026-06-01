namespace Salesync.Application.Modules.Identity.Dtos.Auth
{
    public class RegisterDto
    {
        public required string FullName { get; set; }
        public required string UserName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public int? BranchId { get; set; }
        public int? BusinessUnitId { get; set; }
    }
}
