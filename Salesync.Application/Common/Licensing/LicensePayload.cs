namespace Salesync.Application.Common.Licensing
{
    public class LicensePayload
    {
        public string CompanyId { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ShortName { get; set; } = string.Empty;

        public string? TaxRegistrationNumber { get; set; }

        public string? CommercialRegistrationNumber { get; set; }

        public string LicenseKey { get; set; } = string.Empty;

        public string Plan { get; set; } = string.Empty;

        public string SubscriptionType { get; set; } = string.Empty;

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public int MaxUsers { get; set; }

        public List<string> EnabledFeatures { get; set; } = new();
    }
}