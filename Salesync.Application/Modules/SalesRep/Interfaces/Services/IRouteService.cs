using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;

namespace Salesync.Application.Modules.SalesRep.Interfaces.Services
{
    public interface IRouteService
    {
        Task<IEnumerable<RouteDto>> GetAllAsync();
        Task<RouteDto> GetByIdAsync(int id);
        Task<RouteDto> CreateAsync(CreateRouteDto dto);
        Task<RouteDto> UpdateAsync(int id, UpdateRouteDto dto);
        Task DeleteAsync(int id);
    }
}
