using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;

namespace Salesync.Application.Modules.SalesRep.Interfaces.Services
{
    public interface ISalesRepService
    {
        Task<IEnumerable<SalesRepDto>> GetAllAsync();
        Task<SalesRepDto> GetByIdAsync(int id);
        Task<SalesRepDto> CreateAsync(CreateSalesRepDto dto);
        Task<SalesRepDto> UpdateAsync(int id, UpdateSalesRepDto dto);
        Task DeleteAsync(int id);
    }
}
