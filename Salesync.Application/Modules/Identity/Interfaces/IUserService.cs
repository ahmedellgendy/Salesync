using Salesync.Application.Modules.Identity.Dtos.User;

namespace Salesync.Application.Modules.Identity.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> CreateSalesRepUserAsync(CreateSalesRepUserDto createSalesRepUserDto);
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto> GetUserByIdAsync(string id);
        Task<UserDto> UpdateUserAsync(string id, UpdateUserDto updateUserDto);
        Task<UserDto> SetUserActiveStatusAsync(string id, bool isActive);
        Task ResetUserPasswordAsync(string id,string newPassword);
        Task DeleteUserAsync(string id);
    }
}
