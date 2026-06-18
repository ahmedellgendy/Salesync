using AutoMapper;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class RouteCustomerService : IRouteCustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public RouteCustomerService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<RouteCustomerDto>> GetByRouteIdAsync(int routeId)
        {
            var routeCustomers = await _unitOfWork.RouteCustomers.FindAsync(rc => rc.RouteId == routeId);
            return _mapper.Map<IEnumerable<RouteCustomerDto>>(routeCustomers);
        }
        public async Task<RouteCustomerDto> CreateAsync(CreateRouteCustomerDto dto)
        {
            // Validate route existence
            var routeExists = await _unitOfWork.Routes.ExistsAsync(r => r.Id == dto.RouteId);
            if (!routeExists)
                throw new KeyNotFoundException($"Route with id {dto.RouteId} not found.");

            // Validate customer existence
            var customerExists = await _unitOfWork.Customers.ExistsAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
                throw new KeyNotFoundException($"Customer with id {dto.CustomerId} not found.");

            // visit sequence 
            var routeCustomers = await _unitOfWork.RouteCustomers.FindAsync(rc => rc.RouteId == dto.RouteId);
            int maxSequence = routeCustomers.Any()
                ? routeCustomers.Max(rc => rc.VisitSequence) : 0;
            int newSequence = maxSequence + 1;  

            var routeCustomer = _mapper.Map<RouteCustomer>(dto);
            routeCustomer.VisitSequence = newSequence;

            await _unitOfWork.RouteCustomers.AddAsync(routeCustomer);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<RouteCustomerDto>(routeCustomer);
        }
        public async Task<RouteCustomerDto> UpdateAsync(int id, UpdateRouteCustomerDto dto)
        {
            var routeCustomer = await _unitOfWork.RouteCustomers.GetByIdAsync(id)
               ?? throw new KeyNotFoundException($"RouteCustomer with id {id} not found.");

            _mapper.Map(dto, routeCustomer);
            _unitOfWork.RouteCustomers.Update(routeCustomer);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<RouteCustomerDto>(routeCustomer);
        }
        public async Task DeleteAsync(int id)
        {
            var routeCustomer = await _unitOfWork.RouteCustomers.GetByIdAsync(id)
                 ?? throw new KeyNotFoundException($"RouteCustomer with id {id} not found.");

            _unitOfWork.RouteCustomers.Delete(routeCustomer);
            await _unitOfWork.CompleteAsync();
        }

    }
}
