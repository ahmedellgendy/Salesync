using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Licensing;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Licensing
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseFileProvider _licenseFileProvider;
        private readonly ILicenseSignatureVerifier _signatureVerifier;
        private readonly UserManager<ApplicationUser> _userManager;

        public LicenseService(
            ILicenseFileProvider licenseFileProvider,
            ILicenseSignatureVerifier signatureVerifier,
                UserManager<ApplicationUser> userManager)

        {
            _licenseFileProvider =
                licenseFileProvider;

            _signatureVerifier =
                signatureVerifier;

            _userManager = userManager;
        }


        public LicenseStatus GetStatus()
        {
            SignedLicenseDocument document;

            try
            {
                document =
                    _licenseFileProvider.Load();
            }
            catch (Exception ex)
            {
                return new LicenseStatus
                {
                    IsConfigured = false,
                    IsValid = false,
                    Message = ex.Message
                };
            }


            var payload =
                document.Payload;


            var signatureValid =
                _signatureVerifier.Verify(
                    payload,
                    document.Signature);


            if (!signatureValid)
            {
                return new LicenseStatus
                {
                    IsConfigured = true,
                    IsValid = false,
                    CompanyId = payload.CompanyId,
                    Plan = payload.Plan,
                    SubscriptionType =payload.SubscriptionType,
                    ValidFrom = payload.ValidFrom,
                    ValidTo = payload.ValidTo,
                    MaxUsers = payload.MaxUsers,
                    Message = "Salesync license signature is invalid."
                };
            }


            if (string.IsNullOrWhiteSpace(
                    payload.CompanyId))
            {
                return Invalid(
                    payload,
                    "License company id is missing.");
            }


            if (string.IsNullOrWhiteSpace(
                    payload.LicenseKey))
            {
                return Invalid(
                    payload,
                    "License key is missing.");
            }


            if (payload.MaxUsers <= 0)
            {
                return Invalid(
                    payload,
                    "License maximum users must be greater than zero.");
            }


            var validFrom =
                payload.ValidFrom.Date;

            var validTo =
                payload.ValidTo.Date;


            if (validTo < validFrom)
            {
                return Invalid(
                    payload,
                    "License validity period is invalid.");
            }


            var today =
                DateTime.UtcNow.Date;


            var hasStarted =
                today >= validFrom;

            var isExpired =
                today > validTo;


            return new LicenseStatus
            {
                IsConfigured = true,

                IsValid =
                    hasStarted &&
                    !isExpired,

                HasStarted =
                    hasStarted,

                IsExpired =
                    isExpired,

                CompanyId =
                    payload.CompanyId,

                Plan =
                    payload.Plan,

                SubscriptionType =
                  payload.SubscriptionType,

                ValidFrom =
                    payload.ValidFrom,

                ValidTo =
                    payload.ValidTo,

                MaxUsers =
                    payload.MaxUsers,

                Message =
                    !hasStarted
                        ? "Salesync license has not started yet."
                        : isExpired
                            ? "Salesync license has expired."
                            : "Salesync license is valid."
            };
        }
        public async Task<LicenseStatusDto> GetDetailedStatusAsync()
        {
            var status =
                GetStatus();

            var activeUsers =
                await _userManager.Users
                    .CountAsync(x => x.IsActive);

            var enabledFeatures =
                new List<string>();


            if (status.IsValid)
            {
                var document =
                    _licenseFileProvider.Load();

                enabledFeatures =
                    document.Payload.EnabledFeatures
                        .OrderBy(x => x)
                        .ToList();
            }


            var remainingUsers =
                status.MaxUsers > 0
                    ? Math.Max(
                        0,
                        status.MaxUsers - activeUsers)
                    : 0;


            return new LicenseStatusDto
            {
                IsValid =
                    status.IsValid,

                CompanyId =
                    status.CompanyId,

                Plan =
                    status.Plan,

                SubscriptionType =
                    status.SubscriptionType,

                ValidFrom =
                    status.ValidFrom,

                ValidTo =
                    status.ValidTo,

                MaxUsers =
                    status.MaxUsers,

                ActiveUsers =
                    activeUsers,

                RemainingUsers =
                    remainingUsers,

                EnabledFeatures =
                    enabledFeatures,

                Message =
                    status.Message ?? string.Empty
            };
        }

        public void EnsureLicenseIsValid()
        {
            var status =
                GetStatus();


            if (!status.IsConfigured)
            {
                throw new InvalidOperationException(
                    status.Message ??
                    "Salesync license is not configured.");
            }


            if (!status.IsValid)
            {
                throw new InvalidOperationException(
                    status.Message ??
                    "Salesync license is invalid.");
            }
        }


        public void EnsureCanAddActiveUser(
            int currentActiveUsers)
        {
            if (currentActiveUsers < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentActiveUsers));
            }


            EnsureLicenseIsValid();


            var document =
                _licenseFileProvider.Load();


            if (currentActiveUsers >=
                document.Payload.MaxUsers)
            {
                throw new InvalidOperationException(
                    $"The current license allows a maximum of {document.Payload.MaxUsers} active users.");
            }
        }


        public bool IsFeatureEnabled(
            string feature)
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                return false;
            }


            var status =
                GetStatus();


            if (!status.IsValid)
            {
                return false;
            }


            var document =
                _licenseFileProvider.Load();


            return document.Payload.EnabledFeatures.Any(
                x => string.Equals(
                    x,
                    feature,
                    StringComparison.OrdinalIgnoreCase));
        }


        private static LicenseStatus Invalid(
            LicensePayload payload,
            string message)
        {
            return new LicenseStatus
            {
                IsConfigured = true,
                IsValid = false,
                CompanyId = payload.CompanyId,
                Plan = payload.Plan,
                SubscriptionType = payload.SubscriptionType,
                ValidFrom = payload.ValidFrom,
                ValidTo = payload.ValidTo,
                MaxUsers = payload.MaxUsers,
                Message = message
            };
        }
    }
}