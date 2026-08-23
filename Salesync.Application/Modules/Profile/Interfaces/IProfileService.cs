using Salesync.Application.Modules.Profile.Dtos;

namespace Salesync.Application.Modules.Profile.Interfaces
{
    public interface IProfileService
    {
        Task<CurrentUserProfileDto> GetCurrentUserAsync();
    }
}