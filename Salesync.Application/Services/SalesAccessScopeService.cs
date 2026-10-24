using Microsoft.EntityFrameworkCore;
using Salesync.Application.Common.Exceptions;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;

namespace Salesync.Application.Services;

/// Request-scoped authorization. Claims identify the actor; database relationships
/// determine the current representative/team. Never trusts a submitted rep ID.
public sealed class SalesAccessScopeService(IUnitOfWork unit, ICurrentUserService actor) : ISalesAccessScopeService
{
    private Task<SalesAccessScope>? _scope;
    public Task<SalesAccessScope> GetAsync() => _scope ??= ResolveAsync();

    private async Task<SalesAccessScope> ResolveAsync()
    {
        if (string.IsNullOrWhiteSpace(actor.UserId)) throw new UnauthorizedAccessException("Authentication required.");
        var role = actor.Role?.Trim();
        var reps = unit.SalesReps.GetQueryable().AsNoTracking().Where(r => r.IsActive);
        if (role is "Admin" or "Management") return new(true, [], []);
        if (role is "SalesRep" or "Supervisor")
        {
            var own = await reps.SingleOrDefaultAsync(r => r.UserId == actor.UserId)
                ?? throw new UnauthorizedAccessException("An active sales profile is required.");
            reps = role == "SalesRep" ? reps.Where(r => r.Id == own.Id) : reps.Where(r => r.SupervisorId == own.Id);
        }
        else if (role is "Warehouse" or "Treasury")
        {
            if (!actor.BranchId.HasValue) throw new UnauthorizedAccessException("An assigned branch is required.");
            reps = reps.Where(r => r.BranchId == actor.BranchId.Value);
            if (actor.BusinessUnitId.HasValue) reps = reps.Where(r => r.BusinessUnitId == actor.BusinessUnitId);
        }
        else throw new UnauthorizedAccessException("Role is not permitted for sales operations.");
        var allowed = await reps.Select(r => new { r.Id, r.BranchId }).ToListAsync();
        var branches = allowed.Select(r => r.BranchId).Distinct().ToList();
        if (role is "Warehouse" or "Treasury" && actor.BranchId.HasValue) branches.Add(actor.BranchId.Value);
        return new(false, allowed.Select(r => r.Id).ToArray(), branches.Distinct().ToArray());
    }

    public async Task RequireSalesRepAsync(int? salesRepId)
    {
        var scope = await GetAsync();
        if (!scope.AllowsAll && (!salesRepId.HasValue || !scope.SalesRepIds.Contains(salesRepId.Value)))
            throw new ForbiddenException("Sales representative is outside your authorized scope.");
    }

    public async Task RequireWarehouseAsync(int warehouseId)
    {
        var scope = await GetAsync();
        var warehouse = await unit.Warehouses.GetQueryable().AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == warehouseId && w.IsActive)
            ?? throw new KeyNotFoundException("Warehouse not found.");
        if (!scope.AllowsAll && !scope.BranchIds.Contains(warehouse.BranchId))
            throw new ForbiddenException("Warehouse is outside your authorized scope.");
    }

    public async Task RequireCustomerAsync(int customerId, int salesRepId)
    {
        await RequireSalesRepAsync(salesRepId);
        if ((await GetAsync()).AllowsAll) return;
        var assigned = await unit.RouteCustomers.GetQueryable().AsNoTracking().AnyAsync(rc =>
            rc.CustomerId == customerId && rc.IsActive && rc.Route.IsActive && (rc.Route.AssignedSalesRepId == salesRepId || rc.Route.SalesRepRoutes.Any(a => a.SalesRepId == salesRepId && a.IsActive && a.IsCurrent && !a.UnassignedDate.HasValue)));
        if (!assigned) throw new ForbiddenException("Customer is not assigned to the representative's active routes.");
    }
}
