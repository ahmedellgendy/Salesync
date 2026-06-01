using Salesync.Application.Modules.Identity.Dtos.Auth;

namespace Salesync.Application.Modules.Identity.Interfaces
{
    public interface IAuthService
    {
        Task<TokenDto> LoginAsync(LoginDto loginDto);        
    }
}
