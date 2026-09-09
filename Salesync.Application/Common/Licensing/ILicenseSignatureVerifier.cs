namespace Salesync.Application.Common.Licensing
{
    public interface ILicenseSignatureVerifier
    {
        bool Verify(
            LicensePayload payload,
            string signature);
    }
}