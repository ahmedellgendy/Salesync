using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Profile.Dtos;
using Salesync.Application.Modules.Profile.Interfaces;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Modules.Identity.Services
{
    public sealed class ProfileService : IProfileService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;

        public ProfileService(
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
        }

        public async Task<CurrentUserProfileDto> GetCurrentUserAsync()
        {
            var userId = _currentUserService.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new UnauthorizedAccessException(
                    "Authenticated user id was not found.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                throw new KeyNotFoundException(
                    "Authenticated user was not found.");
            }

            var salesRep = await _unitOfWork.SalesReps
                .GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            var branchId =
                user.BranchId
                ?? salesRep?.BranchId
                ?? _currentUserService.BranchId;

            var businessUnitId =
                user.BusinessUnitId
                ?? salesRep?.BusinessUnitId
                ?? _currentUserService.BusinessUnitId;

            string? branchName = null;

            if (branchId.HasValue)
            {
                branchName = await _unitOfWork.Branches
                    .GetQueryable()
                    .AsNoTracking()
                    .Where(x => x.Id == branchId.Value)
                    .Select(x => x.Name)
                    .FirstOrDefaultAsync();
            }

            return new CurrentUserProfileDto
            {
                UserId = user.Id,

                FullName = user.FullName,

                UserName = user.UserName,

                Email = user.Email,

                Phone =
                    user.PhoneNumber
                    ?? salesRep?.Phone,

                Role =
                    _currentUserService.Role
                    ?? string.Empty,

                BranchId = branchId,

                BranchName = branchName,

                BusinessUnitId = businessUnitId,

                SalesRepId = salesRep?.Id,

                SalesRepCode = salesRep?.SalesRepCode,

                ProfileImageUrl = salesRep?.ProfileImageUrl
            };
        }
    }
}