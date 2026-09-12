namespace Salesync.Domain.Modules.DataImport.Enums
{
    public enum ImportStatus
    {
        Uploaded = 1,
        Validating = 2,
        ValidationFailed = 3,
        ReadyToImport = 4,
        Importing = 5,
        Completed = 6,
        Failed = 7
    }
}