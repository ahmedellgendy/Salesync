using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;

namespace Salesync.Application.Modules.MasterData.Interfaces.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int id);
        Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto);
        Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto);
        Task DeleteAsync(int id);

    }
}
