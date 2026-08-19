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

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedAccessException(
                    "Authenticated user was not found.");

            var supervisor = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.UserId == userId && x.IsActive,
                    cancellationToken);

            if (supervisor is null)
                throw new KeyNotFoundException(
                    "Supervisor profile for the current user was not found.");

            var allowedSalesRepIds = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .Where(x =>
                    x.SupervisorId == supervisor.Id &&
                    x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            return new ReportScope
            {
                AllowedSalesRepIds = allowedSalesRepIds
            };
        }
    }
}