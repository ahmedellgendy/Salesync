using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Reports.Common.Interfaces;
using Salesync.Application.Modules.Reports.Common.Scopes;

namespace Salesync.Application.Modules.Reports.Common.Services
{
    public class ReportScopeService : IReportScopeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;

        public ReportScopeService(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
        }

        public async Task<ReportScope> GetCurrentScopeAsync(CancellationToken cancellationToken = default)
        {
            var userId = _currentUser.UserId;
            var role = _currentUser.Role;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");
            }

            if (string.IsNullOrWhiteSpace(role))
            {
                throw new UnauthorizedAccessException(
                    "User role was not found.");
            }

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase) || role.Equals("Management", StringComparison.OrdinalIgnoreCase))
            {
                return await BuildFullBusinessScopeAsync(
                    cancellationToken);
            }

            if (role.Equals(
                    "Supervisor",
                    StringComparison.OrdinalIgnoreCase))
            {
                return await BuildSupervisorScopeAsync(
                    userId,
                    cancellationToken);
            }

            throw new UnauthorizedAccessException(
                "The current user is not allowed to access reports.");
        }

        private async Task<ReportScope> BuildSupervisorScopeAsync(string userId, CancellationToken cancellationToken)
        {
            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x =>
                        x.UserId == userId &&
                        x.IsActive,
                    cancellationToken);

            if (supervisor is null)
            {
                throw new KeyNotFoundException(
                    "Supervisor profile for the current user was not found.");
            }

            var allowedSalesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SupervisorId == supervisor.Id &&
                    x.IsActive)
                .Select(x => new
                {
                    x.Id,
                    x.BranchId
                })
                .ToListAsync(cancellationToken);

            return new ReportScope
            {
                AllowedSalesRepIds = allowedSalesReps
                    .Select(x => x.Id)
                    .ToList(),

                AllowedBranchIds = allowedSalesReps
                    .Select(x => x.BranchId)
                    .Distinct()
                    .ToList()
            };
        }

        private async Task<ReportScope> BuildFullBusinessScopeAsync(CancellationToken cancellationToken)
        {
            var allowedSalesReps = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.SupervisorId.HasValue)
                .Select(x => new
                {
                    x.Id,
                    x.BranchId,
                    x.BusinessUnitId
                })
                .ToListAsync(cancellationToken);

            return new ReportScope
            {
                AllowedSalesRepIds = allowedSalesReps
                    .Select(x => x.Id)
                    .ToList(),

                AllowedBranchIds = allowedSalesReps
                    .Select(x => x.BranchId)
                    .Distinct()
                    .ToList(),

                AllowedBusinessUnitIds = allowedSalesReps
                    .Where(x => x.BusinessUnitId.HasValue)
                    .Select(x => x.BusinessUnitId!.Value)
                    .Distinct()
                    .ToList()
            };
        }
    }
}