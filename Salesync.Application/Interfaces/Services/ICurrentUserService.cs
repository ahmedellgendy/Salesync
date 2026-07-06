namespace Salesync.Application.Interfaces.Services
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? Role { get; }
        int? SalesRepId { get; }
        int? BranchId { get; }
        int? BusinessUnitId { get; }
    }
}
