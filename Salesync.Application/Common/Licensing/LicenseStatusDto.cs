namespace Salesync.Application.Common.Licensing
{
    public class LicenseStatusDto
    {
        public bool IsValid { get; set; }

        public string CompanyId { get; set; } = string.Empty;

        public string Plan { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public int MaxUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int RemainingUsers { get; set; }

        public List<string> EnabledFeatures { get; set; } = new();

        public string Message { get; set; } = string.Empty;
    }
}