namespace Salesync.Application.Interfaces.Services;

public sealed record SalesAccessScope(bool AllowsAll, int[] SalesRepIds, int[] BranchIds);

public interface ISalesAccessScopeService
{
    Task<SalesAccessScope> GetAsync();
    Task RequireSalesRepAsync(int? salesRepId);
    Task RequireWarehouseAsync(int warehouseId);
    Task RequireCustomerAsync(int customerId, int salesRepId);
}
