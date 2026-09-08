namespace Salesync.Application.Common.Licensing
{
    public interface ILicenseService
    {
        LicenseStatus GetStatus();

        void EnsureLicenseIsValid();

        void EnsureCanAddActiveUser(int currentActiveUsers);

        bool IsFeatureEnabled(string feature);
    }
}