namespace Salesync.Application.Modules.Identity.Dtos.Auth
{
    public class LoginDto
    {
        public required string UserName { get; set; }
        public required string Password { get; set; }
    }
}
