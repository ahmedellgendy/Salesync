using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Modules.SalesRep.Entities;
using Salesync.Application.Common.Exceptions;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateRouteDto> _createRouteValidator;
        private readonly IValidator<UpdateRouteDto> _updateRouteValidator;

        public RouteService(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateRouteDto> createRouteValidator, IValidator<UpdateRouteDto> updateRouteValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createRouteValidator = createRouteValidator;
            _updateRouteValidator = updateRouteValidator;
        }

        public async Task<IEnumerable<RouteDto>> GetAllAsync()
        {
            var routes = await _unitOfWork.Routes.FindAsync(r => r.IsActive);
            return _mapper.Map<IEnumerable<RouteDto>>(routes);
        }
        public async Task<RouteDto> GetByIdAsync(int id)
        {
            var route = await _unitOfWork.Routes.FindAsync(r => r.Id == id && r.IsActive)
                ?? throw new KeyNotFoundException($"Route with id {id} not found.");
            return _mapper.Map<RouteDto>(route.FirstOrDefault());
        }
        public async Task<IEnumerable<RouteDto>> GetBySalesRepAsync(int salesRepId, string userId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException("Authenticated user was not found.");

            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive);

            if (supervisor is null)
                throw new KeyNotFoundException("Supervisor profile was not found.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == salesRepId &&
                    x.IsActive);

            if (salesRep is null)
                throw new KeyNotFoundException($"SalesRep with id {salesRepId} was not found.");

            if (salesRep.SupervisorId != supervisor.Id)
                throw new ForbiddenException("You are not allowed to access this sales rep.");

            var routes = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.AssignedSalesRepId == salesRepId)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteDto>>(routes);
        }
        public async Task<RouteDto> CreateAsync(CreateRouteDto dto)
        {
            // FluentValidation - Validate data format
            var validationResult = await _createRouteValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            // Check if the route code already exists
            var routeCodeExists = await _unitOfWork.Routes.ExistsAsync(x => x.RouteCode == dto.RouteCode);
            if (routeCodeExists)
                throw new InvalidOperationException($"Route with code {dto.RouteCode} already exists.");

            // Check if the branch exists
            var branchExists = await _unitOfWork.Branches.ExistsAsync(x => x.Id == dto.BranchId);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch with id {dto.BranchId} not found.");

            var route = _mapper.Map<Route>(dto);

            await _unitOfWork.Routes.AddAsync(route);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<RouteDto>(route);
        }
        public async Task<RouteDto> UpdateAsync(int id, UpdateRouteDto dto)
        {
            // FluentValidation - Validate data format
            var validationResult = await _updateRouteValidator.ValidateAsync(dto);
            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var route = await _unitOfWork.Routes.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Route with id {id} not found.");

            _mapper.Map(dto, route);
            _unitOfWork.Routes.Update(route);
            await _unitOfWork.CompleteAsync();
            return _mapper.Map<RouteDto>(route);
        }
        public async Task DeleteAsync(int id)
        {
            var route = await _unitOfWork.Routes.GetByIdAsync(id)
                 ?? throw new KeyNotFoundException($"Route with id {id} not found.");

            route.IsActive = false;
            _unitOfWork.Routes.Delete(route);
            await _unitOfWork.CompleteAsync();
        }
    }
}
