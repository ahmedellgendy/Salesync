namespace Salesync.Application.Common.Licensing
{
    public class SignedLicenseDocument
    {
        public LicensePayload Payload { get; set; } = new();

        public string Signature { get; set; } = string.Empty;
    }
}