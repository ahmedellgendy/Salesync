namespace Salesync.Application.Common.Licensing
{
    public class LicenseStatus
    {
        public bool IsConfigured { get; set; }

        public bool IsValid { get; set; }

        public bool IsExpired { get; set; }

        public bool HasStarted { get; set; }

        public string CompanyId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public string? TaxRegistrationNumber { get; set; }

        public string? CommercialRegistrationNumber { get; set; }

        public string Plan { get; set; } = string.Empty;

        public string SubscriptionType { get; set; } = string.Empty;

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public int MaxUsers { get; set; }

        public string? Message { get; set; }
    }
}