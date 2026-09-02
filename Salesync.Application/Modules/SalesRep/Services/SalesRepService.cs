using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Identity.Dtos.User;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Application.Modules.SalesRep.Dtos.SalesRepDto;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;

namespace Salesync.Application.Modules.SalesRep.Services
{
    public class SalesRepService : ISalesRepService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IValidator<UpdateSalesRepDto> _updateSalesRepValidator;

        public SalesRepService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IUserService userService,
            IValidator<UpdateSalesRepDto> updateSalesRepValidator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _userService = userService;
            _updateSalesRepValidator = updateSalesRepValidator;
        }

        public async Task<IEnumerable<SalesRepDto>> GetAllAsync()
        {
            var salesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Include(x => x.Branch)
                .Include(x => x.Supervisor)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepDto>>(salesReps);
        }

        public async Task<IEnumerable<SalesRepDto>> GetMyTeamAsync(
            string userId)
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
            {
                throw new KeyNotFoundException(
                    "Supervisor profile was not found.");
            }

            var teamMembers = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SupervisorId == supervisor.Id &&
                    x.IsActive)
                .Include(x => x.Branch)
                .Include(x => x.Supervisor)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return _mapper.Map<IEnumerable<SalesRepDto>>(teamMembers);
        }

        public async Task<SalesRepDto> GetByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Include(x => x.Branch)
                .Include(x => x.Supervisor)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (salesRep is null)
            {
                throw new KeyNotFoundException(
                    $"SalesRep with ID {id} not found.");
            }

            return _mapper.Map<SalesRepDto>(salesRep);
        }

        public async Task<SalesRepDto> UpdateAsync(
            int id,
            UpdateSalesRepDto dto)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            var validationResult =
                await _updateSalesRepValidator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                throw new ValidationException(
                    validationResult.Errors);
            }

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .Include(x => x.Branch)
                .Include(x => x.Supervisor)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (salesRep is null)
            {
                throw new KeyNotFoundException(
                    $"SalesRep with ID {id} not found.");
            }

            if (dto.BranchId.HasValue)
            {
                var branchExists = await _unitOfWork.Branches
                    .ExistsAsync(x =>
                        x.Id == dto.BranchId.Value &&
                        x.IsActive);

                if (!branchExists)
                {
                    throw new KeyNotFoundException(
                        $"Active branch with ID {dto.BranchId.Value} was not found.");
                }
            }

            if (dto.SupervisorId.HasValue)
            {
                if (dto.SupervisorId.Value == id)
                {
                    throw new InvalidOperationException(
                        "A sales representative cannot be their own supervisor.");
                }

                var supervisor = await _unitOfWork.SalesReps
                    .GetQueryable()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == dto.SupervisorId.Value &&
                        x.IsActive);

                if (supervisor is null)
                {
                    throw new KeyNotFoundException(
                        $"Active supervisor with ID {dto.SupervisorId.Value} was not found.");
                }

                if (dto.BranchId.HasValue &&
                    supervisor.BranchId != dto.BranchId.Value)
                {
                    throw new InvalidOperationException(
                        "The supervisor must belong to the same branch as the sales representative.");
                }
            }

            NormalizeUpdateDto(dto);

            _mapper.Map(dto, salesRep);

            salesRep.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.SalesReps.Update(salesRep);

            await _unitOfWork.CompleteAsync();

            // Keep linked Identity user aligned with SalesRep master data.
            if (!string.IsNullOrWhiteSpace(salesRep.UserId))
            {
                await _userService.UpdateUserAsync(
                    salesRep.UserId,
                    new UpdateUserDto
                    {
                        FullName = salesRep.Name,
                        BranchId = salesRep.BranchId,
                        BusinessUnitId = salesRep.BusinessUnitId
                    });
            }

            return await GetByIdAsync(id);
        }

        public async Task DeleteAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Invalid sales rep id.");

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.IsActive);

            if (salesRep is null)
            {
                throw new KeyNotFoundException(
                    $"SalesRep with ID {id} not found.");
            }

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                salesRep.IsActive = false;
                salesRep.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.SalesReps.Update(salesRep);

                await _unitOfWork.CompleteAsync();

                if (!string.IsNullOrWhiteSpace(salesRep.UserId))
                {
                    await _userService.DeleteUserAsync(
                        salesRep.UserId);
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private static void NormalizeUpdateDto(
            UpdateSalesRepDto dto)
        {
            dto.Name = NormalizeOptional(dto.Name);
            dto.Phone = NormalizeOptional(dto.Phone);
            dto.Mobile = NormalizeOptional(dto.Mobile);

            dto.Email = NormalizeOptional(dto.Email)?
                .ToLowerInvariant();

            dto.Address = NormalizeOptional(dto.Address);
            dto.CategoryCode =
                NormalizeOptional(dto.CategoryCode);
        }

        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}