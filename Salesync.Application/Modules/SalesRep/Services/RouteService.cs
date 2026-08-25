using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Exceptions;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.SalesRep.Dtos.RouteDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Domain.Modules.SalesRep.Entities;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class RouteService : IRouteService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateRouteDto> _createRouteValidator;
        private readonly IValidator<UpdateRouteDto> _updateRouteValidator;

        public RouteService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IValidator<CreateRouteDto> createRouteValidator,
            IValidator<UpdateRouteDto> updateRouteValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _createRouteValidator = createRouteValidator;
            _updateRouteValidator = updateRouteValidator;
        }

        public async Task<IEnumerable<RouteDto>> GetAllAsync()
        {
            var routes = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Branch)
                .Include(x => x.AssignedSalesRep)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteDto>>(routes);
        }

        public async Task<RouteDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid route id.");

            var route = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.AssignedSalesRep)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (route is null)
                throw new KeyNotFoundException(
                    $"Route with id {id} not found.");

            return _mapper.Map<RouteDto>(route);
        }

        public async Task<IEnumerable<RouteDto>> GetBySalesRepAsync(
            int salesRepId,
            string userId)
        {
            if (salesRepId <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");

            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.UserId == userId &&
                    x.IsActive);

            if (supervisor is null)
                throw new KeyNotFoundException(
                    "Supervisor profile was not found.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == salesRepId &&
                    x.IsActive);

            if (salesRep is null)
                throw new KeyNotFoundException(
                    $"SalesRep with id {salesRepId} was not found.");

            if (salesRep.SupervisorId != supervisor.Id)
                throw new ForbiddenException(
                    "You are not allowed to access this sales rep.");

            var routes = await _unitOfWork.Routes
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.AssignedSalesRepId == salesRepId &&
                    x.IsActive)
                .Include(x => x.Branch)
                .Include(x => x.AssignedSalesRep)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<RouteDto>>(routes);
        }

        public async Task<RouteDto> CreateAsync(CreateRouteDto dto)
        {
            var validationResult =
                await _createRouteValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            NormalizeCreateDto(dto);

            var routeCodeExists =
                await _unitOfWork.Routes.ExistsAsync(x =>
                    x.RouteCode == dto.RouteCode);

            if (routeCodeExists)
                throw new InvalidOperationException(
                    $"Route with code '{dto.RouteCode}' already exists.");

            var branchExists =
                await _unitOfWork.Branches.ExistsAsync(x =>
                    x.Id == dto.BranchId &&
                    x.IsActive);

            if (!branchExists)
                throw new KeyNotFoundException(
                    $"Active branch with id {dto.BranchId} was not found.");

            if (dto.AssignedSalesRepId.HasValue)
            {
                await ValidateAssignedSalesRepAsync(
                    dto.AssignedSalesRepId.Value,
                    dto.BranchId);
            }

            var route = _mapper.Map<Route>(dto);

            route.IsActive = true;
            route.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Routes.AddAsync(route);
            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(route.Id);
        }

        public async Task<RouteDto> UpdateAsync(
            int id,
            UpdateRouteDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid route id.");

            var validationResult =
                await _updateRouteValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);

            var route = await _unitOfWork.Routes
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (route is null)
                throw new KeyNotFoundException(
                    $"Route with id {id} not found.");

            var finalBranchId =
                dto.BranchId ?? route.BranchId;

            if (dto.BranchId.HasValue)
            {
                var branchExists =
                    await _unitOfWork.Branches.ExistsAsync(x =>
                        x.Id == dto.BranchId.Value &&
                        x.IsActive);

                if (!branchExists)
                    throw new KeyNotFoundException(
                        $"Active branch with id {dto.BranchId.Value} was not found.");
            }

            if (dto.AssignedSalesRepId.HasValue)
            {
                await ValidateAssignedSalesRepAsync(
                    dto.AssignedSalesRepId.Value,
                    finalBranchId);
            }
            else if (route.AssignedSalesRepId.HasValue &&
                     dto.BranchId.HasValue)
            {
                // Existing assigned rep must still belong to the new branch.
                await ValidateAssignedSalesRepAsync(
                    route.AssignedSalesRepId.Value,
                    finalBranchId);
            }

            NormalizeUpdateDto(dto);

            _mapper.Map(dto, route);

            route.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Routes.Update(route);

            await _unitOfWork.CompleteAsync();

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid route id.");

            var route = await _unitOfWork.Routes
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (route is null)
                throw new KeyNotFoundException(
                    $"Route with id {id} not found.");

            route.IsActive = false;
            route.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Routes.Update(route);

            await _unitOfWork.CompleteAsync();
        }

        private async Task ValidateAssignedSalesRepAsync(
            int salesRepId,
            int branchId)
        {
            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == salesRepId &&
                    x.IsActive);

            if (salesRep is null)
                throw new KeyNotFoundException(
                    $"Active SalesRep with id {salesRepId} was not found.");

            if (salesRep.BranchId != branchId)
                throw new InvalidOperationException(
                    "The assigned sales representative must belong to the same branch as the route.");
        }

        private static void NormalizeCreateDto(CreateRouteDto dto)
        {
            dto.RouteCode = dto.RouteCode.Trim();
            dto.Name = dto.Name.Trim();

            dto.Type = NormalizeOptional(dto.Type);
            dto.RegionCode = NormalizeOptional(dto.RegionCode);
            dto.DistrictCode = NormalizeOptional(dto.DistrictCode);
            dto.CityCode = NormalizeOptional(dto.CityCode);
            dto.AreaCode = NormalizeOptional(dto.AreaCode);
            dto.RouteChannel = NormalizeOptional(dto.RouteChannel);
            dto.RouteGTM = NormalizeOptional(dto.RouteGTM);
            dto.RouteCategory = NormalizeOptional(dto.RouteCategory);
        }

        private static void NormalizeUpdateDto(UpdateRouteDto dto)
        {
            dto.Name = NormalizeOptional(dto.Name);
            dto.Type = NormalizeOptional(dto.Type);
            dto.RegionCode = NormalizeOptional(dto.RegionCode);
            dto.DistrictCode = NormalizeOptional(dto.DistrictCode);
            dto.CityCode = NormalizeOptional(dto.CityCode);
            dto.AreaCode = NormalizeOptional(dto.AreaCode);
            dto.RouteChannel = NormalizeOptional(dto.RouteChannel);
            dto.RouteGTM = NormalizeOptional(dto.RouteGTM);
            dto.RouteCategory = NormalizeOptional(dto.RouteCategory);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}