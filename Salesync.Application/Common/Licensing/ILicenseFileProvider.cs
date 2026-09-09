namespace Salesync.Application.Common.Licensing
{
    public interface ILicenseFileProvider
    {
        SignedLicenseDocument Load();
    }
}