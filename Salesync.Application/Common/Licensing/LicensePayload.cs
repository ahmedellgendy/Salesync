namespace Salesync.Application.Common.Licensing
{
    public class LicensePayload
    {
        public string CompanyId { get; set; } = string.Empty;

        public string LicenseKey { get; set; } = string.Empty;

        public string Plan { get; set; } = string.Empty;

        public string SubscriptionType { get; set; } = string.Empty;

        public DateTime ValidFrom { get; set; }

        public DateTime ValidTo { get; set; }

        public int MaxUsers { get; set; }

        public List<string> EnabledFeatures { get; set; } = new();
    }
}