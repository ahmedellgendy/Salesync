using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;

namespace Salesync.Application.Modules.SalesRep.Interfaces.Services
{
    public interface IRouteCustomerService
    {
        Task<IEnumerable<RouteCustomerDto>> GetByRouteIdAsync(int routeId);
        Task<RouteCustomerDto> CreateAsync(CreateRouteCustomerDto dto);
        Task<RouteCustomerDto> UpdateAsync(int id, UpdateRouteCustomerDto dto);
        Task DeleteAsync(int id);
    }
}
