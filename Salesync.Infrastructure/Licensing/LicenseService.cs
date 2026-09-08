using Salesync.Application.Common.Licensing;

namespace Salesync.Infrastructure.Licensing
{
    public class LicenseService : ILicenseService
    {
        private readonly ILicenseFileProvider _licenseFileProvider;
        private readonly ILicenseSignatureVerifier _signatureVerifier;


        public LicenseService(
            ILicenseFileProvider licenseFileProvider,
            ILicenseSignatureVerifier signatureVerifier)
        {
            _licenseFileProvider =
                licenseFileProvider;

            _signatureVerifier =
                signatureVerifier;
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
                ValidFrom = payload.ValidFrom,
                ValidTo = payload.ValidTo,
                MaxUsers = payload.MaxUsers,
                Message = message
            };
        }
    }
}