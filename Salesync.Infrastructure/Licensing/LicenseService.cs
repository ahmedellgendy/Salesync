using Microsoft.Extensions.Options;
using Salesync.Application.Common.Licensing;
using Salesync.Application.Common.Settings;

namespace Salesync.Infrastructure.Licensing
{
    public class LicenseService : ILicenseService
    {
        private readonly LicenseSettings _settings;

        public LicenseService(
            IOptions<LicenseSettings> options)
        {
            _settings = options.Value;
        }


        public LicenseStatus GetStatus()
        {
            var now =
                DateTime.UtcNow.Date;


            var isConfigured =
                !string.IsNullOrWhiteSpace(_settings.CompanyId)
                &&
                !string.IsNullOrWhiteSpace(_settings.LicenseKey)
                &&
                _settings.ValidFrom.HasValue
                &&
                _settings.ValidTo.HasValue
                &&
                _settings.MaxUsers > 0;


            if (!isConfigured)
            {
                return new LicenseStatus
                {
                    IsConfigured = false,
                    IsValid = false,
                    IsExpired = false,
                    HasStarted = false,
                    CompanyId = _settings.CompanyId,
                    Plan = _settings.Plan,
                    ValidFrom = _settings.ValidFrom,
                    ValidTo = _settings.ValidTo,
                    MaxUsers = _settings.MaxUsers,
                    Message = "Salesync license is not configured."
                };
            }


            var validFrom =
                _settings.ValidFrom!.Value.Date;

            var validTo =
                _settings.ValidTo!.Value.Date;


            if (validTo < validFrom)
            {
                return new LicenseStatus
                {
                    IsConfigured = true,
                    IsValid = false,
                    CompanyId = _settings.CompanyId,
                    Plan = _settings.Plan,
                    ValidFrom = _settings.ValidFrom,
                    ValidTo = _settings.ValidTo,
                    MaxUsers = _settings.MaxUsers,
                    Message = "License validity period is invalid."
                };
            }


            var hasStarted =
                now >= validFrom;

            var isExpired =
                now > validTo;


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
                    _settings.CompanyId,

                Plan =
                    _settings.Plan,

                ValidFrom =
                    _settings.ValidFrom,

                ValidTo =
                    _settings.ValidTo,

                MaxUsers =
                    _settings.MaxUsers,

                Message =
                    !hasStarted
                        ? "License has not started yet."
                        : isExpired
                            ? "License has expired."
                            : "License is valid."
            };
        }


        public void EnsureLicenseIsValid()
        {
            var status =
                GetStatus();


            if (!status.IsConfigured)
            {
                throw new InvalidOperationException(
                    "Salesync license is not configured.");
            }


            if (!status.HasStarted)
            {
                throw new InvalidOperationException(
                    "Salesync license has not started yet.");
            }


            if (status.IsExpired)
            {
                throw new InvalidOperationException(
                    "Salesync license has expired.");
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
            EnsureLicenseIsValid();


            if (currentActiveUsers < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentActiveUsers));
            }


            if (currentActiveUsers >=
                _settings.MaxUsers)
            {
                throw new InvalidOperationException(
                    $"The current license allows a maximum of {_settings.MaxUsers} active users.");
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


            return _settings.EnabledFeatures.Any(x =>
                string.Equals(
                    x,
                    feature,
                    StringComparison.OrdinalIgnoreCase));
        }
    }
}