using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Exceptions;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Modules.SalesRep.Dtos.RouteCustomerDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Common.Enums.MasterData;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class RouteCustomerService : IRouteCustomerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateRouteCustomerDto> _createValidator;
        private readonly IValidator<UpdateRouteCustomerDto> _updateValidator;

        public RouteCustomerService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateRouteCustomerDto> createValidator,
            IValidator<UpdateRouteCustomerDto> updateValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
        }

        public async Task<IEnumerable<RouteCustomerDto>> GetByRouteIdAsync(int routeId)
        {
            if (routeId <= 0)
                throw new ArgumentException("Invalid route id.");

            var routeExists = await _unitOfWork.Routes
                .ExistsAsync(x =>
                    x.Id == routeId &&
                    x.IsActive);

            if (!routeExists)
            {
                throw new KeyNotFoundException(
                    $"Active route with id {routeId} not found.");
            }

            var routeCustomers = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.RouteId == routeId)
                .OrderBy(x => x.VisitSequence)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteCustomerDto>>(routeCustomers);
        }

        public async Task<IEnumerable<RouteCustomerDetailsDto>> GetRouteCustomersAsync(
            int routeId,
            string userId)
        {
            if (routeId <= 0)
                throw new ArgumentException("Invalid route id.");

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");
            }

            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive);

            if (supervisor is null)
                throw new KeyNotFoundException("Supervisor profile was not found.");

            var route = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == routeId &&
                    x.IsActive);

            if (route is null)
                throw new KeyNotFoundException($"Route with id {routeId} not found.");

            if (!route.AssignedSalesRepId.HasValue)
            {
                throw new ForbiddenException(
                    "You are not allowed to access this route.");
            }

            var assignedSalesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == route.AssignedSalesRepId.Value &&
                    x.IsActive);

            if (assignedSalesRep is null)
                throw new KeyNotFoundException("Assigned sales rep was not found.");

            if (assignedSalesRep.SupervisorId != supervisor.Id)
            {
                throw new ForbiddenException(
                    "You are not allowed to access this route.");
            }

            var routeCustomers = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Customer)
                .Where(x =>
                    x.RouteId == routeId &&
                    x.Customer.IsActive)
                .OrderBy(x => x.VisitSequence)
                .ToListAsync();

            return routeCustomers.Select(x => new RouteCustomerDetailsDto
            {
                Id = x.Id,
                RouteId = x.RouteId,
                CustomerId = x.CustomerId,
                VisitSequence = x.VisitSequence,
                VisitDays = x.VisitDays,
                Notes = x.Notes,
                Customer = _mapper.Map<CustomerDto>(x.Customer)
            });
        }

        public async Task<IEnumerable<CustomerDto>> GetMyTeamCustomersAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");
            }

            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive);

            if (supervisor is null)
                throw new KeyNotFoundException("Supervisor profile was not found.");

            var teamSalesRepIds = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SupervisorId == supervisor.Id &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            if (teamSalesRepIds.Count == 0)
                return Enumerable.Empty<CustomerDto>();

            var routeIds = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.AssignedSalesRepId.HasValue &&
                    teamSalesRepIds.Contains(x.AssignedSalesRepId.Value) &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync();

            if (routeIds.Count == 0)
                return Enumerable.Empty<CustomerDto>();

            var customerIds = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    routeIds.Contains(x.RouteId) &&
                    x.Customer.IsActive &&
                    x.Customer.Status == CustomerStatus.Active)
                .Select(x => x.CustomerId)
                .Distinct()
                .ToListAsync();

            if (customerIds.Count == 0)
                return Enumerable.Empty<CustomerDto>();

            var customers = await _unitOfWork.Customers
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    customerIds.Contains(x.Id) &&
                    x.IsActive &&
                    x.Status == CustomerStatus.Active)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CustomerDto>>(customers);
        }

        public async Task<RouteCustomerDto> CreateAsync(CreateRouteCustomerDto dto)
        {
            var validationResult = await _createValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var routeExists = await _unitOfWork.Routes
                .ExistsAsync(x =>
                    x.Id == dto.RouteId &&
                    x.IsActive);

            if (!routeExists)
            {
                throw new KeyNotFoundException(
                    $"Active route with id {dto.RouteId} not found.");
            }

            var customerExists = await _unitOfWork.Customers
                .ExistsAsync(x =>
                    x.Id == dto.CustomerId &&
                    x.IsActive);

            if (!customerExists)
            {
                throw new KeyNotFoundException(
                    $"Active customer with id {dto.CustomerId} not found.");
            }

            var alreadyAssigned = await _unitOfWork.RouteCustomers
                .ExistsAsync(x =>
                    x.RouteId == dto.RouteId &&
                    x.CustomerId == dto.CustomerId);

            if (alreadyAssigned)
            {
                throw new InvalidOperationException(
                    "The customer is already assigned to this route.");
            }

            var maxSequence = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.RouteId == dto.RouteId)
                .Select(x => (int?)x.VisitSequence)
                .MaxAsync() ?? 0;

            var routeCustomer = _mapper.Map<RouteCustomer>(dto);

            routeCustomer.VisitSequence = maxSequence + 1;
            routeCustomer.VisitDays = NormalizeOptional(dto.VisitDays);
            routeCustomer.Notes = NormalizeOptional(dto.Notes);

            routeCustomer.IsActive = true;
            routeCustomer.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.RouteCustomers.AddAsync(routeCustomer);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<RouteCustomerDto>(routeCustomer);
        }

        public async Task<RouteCustomerDto> UpdateAsync(
            int id,
            UpdateRouteCustomerDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid route customer id.");

            var validationResult = await _updateValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var routeCustomer = await _unitOfWork.RouteCustomers
                .GetQueryable()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (routeCustomer is null)
            {
                throw new KeyNotFoundException(
                    $"RouteCustomer with id {id} not found.");
            }

            dto.VisitDays = NormalizeOptional(dto.VisitDays);
            dto.Notes = NormalizeOptional(dto.Notes);

            _mapper.Map(dto, routeCustomer);

            routeCustomer.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.RouteCustomers.Update(routeCustomer);

            await _unitOfWork.CompleteAsync();

            return _mapper.Map<RouteCustomerDto>(routeCustomer);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid route customer id.");

            var routeCustomer = await _unitOfWork.RouteCustomers
                .GetByIdAsync(id)
                ?? throw new KeyNotFoundException(
                    $"RouteCustomer with id {id} not found.");

            _unitOfWork.RouteCustomers.Delete(routeCustomer);

            await _unitOfWork.CompleteAsync();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}