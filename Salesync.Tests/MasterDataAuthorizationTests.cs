using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Salesync.API.Controllers.MasterData;
using Salesync.Application.Modules.MasterData.Services;
using Salesync.Application.Modules.MasterData.Dtos.CustomerDto;
using Salesync.Application.Common.Exceptions;
using Salesync.Domain.Modules.MasterData.Entities;
using Salesync.Domain.Modules.SalesRep.Entities;
using Xunit;

namespace Salesync.Tests;

public class MasterDataAuthorizationTests
{
    [Theory]
    [InlineData(typeof(BranchesController))]
    [InlineData(typeof(CustomersController))]
    [InlineData(typeof(ProductController))]
    [InlineData(typeof(WarehouseController))]
    public async Task Anonymous_requests_are_rejected_for_every_master_data_action(Type controller)
    {
        using var services = new ServiceCollection().AddLogging().AddAuthorizationCore().BuildServiceProvider();
        foreach (var method in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var policy = await Policy(services, controller, method);
            Assert.False((await services.GetRequiredService<IAuthorizationService>()
                .AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), null, policy)).Succeeded);
        }
    }

    [Theory]
    [InlineData("SalesRep")]
    [InlineData("Supervisor")]
    [InlineData("Warehouse")]
    [InlineData("Treasury")]
    [InlineData("Management")]
    [InlineData("Admin")]
    public async Task Existing_authenticated_read_roles_remain_allowed_while_mutations_are_restricted(string role)
    {
        using var services = new ServiceCollection().AddLogging().AddAuthorizationCore().BuildServiceProvider();
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Role, role)], "Mock"));
        foreach (var type in new[] { typeof(BranchesController), typeof(CustomersController), typeof(ProductController), typeof(WarehouseController) })
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var read = method.Name.StartsWith("Get");
            var allowed = read || role == "Admin" || (type == typeof(CustomersController) && role == "Supervisor" && method.Name is "Create" or "Update");
            var policy = await Policy(services, type, method);
            Assert.Equal(allowed, (await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(actor, null, policy)).Succeeded);
        }
    }

    private static async Task<AuthorizationPolicy> Policy(IServiceProvider services, Type type, MethodInfo method) =>
        (await AuthorizationPolicy.CombineAsync(services.GetRequiredService<IAuthorizationPolicyProvider>(),
            type.GetCustomAttributes<AuthorizeAttribute>().Concat(method.GetCustomAttributes<AuthorizeAttribute>())))!;

    [Theory]
    [InlineData("SalesRep", 1)]
    [InlineData("Supervisor", 2)]
    [InlineData("Warehouse", 2)]
    [InlineData("Treasury", 2)]
    [InlineData("Admin", 3)]
    [InlineData("Management", 3)]
    public async Task Customer_scope_keeps_assigned_legacy_customers_and_hides_other_branches(string role, int count)
    {
        await using var f = await WorkflowFixture.CreateAsync();
        f.Actor.Role = role; if (role == "Supervisor") f.Actor.UserId = "supervisor";
        f.Db.AddRange(new Customer { Id = 2, Name = "Other", BranchId = 2 }, new Customer { Id = 3, Name = "Local", BranchId = 1 });
        f.Db.Add(new Route { Id = 1, RouteCode = "R1", Name = "Route", BranchId = 1, AssignedSalesRepId = 1 });
        f.Db.Add(new RouteCustomer { RouteId = 1, CustomerId = 1 });
        await f.Db.SaveChangesAsync();
        var service = ActivatorUtilities.CreateInstance<CustomerService>(f.Services);
        Assert.Equal(count, (await service.GetAllAsync()).Count());
        Assert.NotNull(await service.GetByIdAsync(1)); // Legacy customer has no BranchId, but is assigned.
        if (role is not ("Admin" or "Management")) Assert.Null(await service.GetByIdAsync(2));
    }

    [Theory]
    [InlineData("SalesRep")]
    [InlineData("Supervisor")]
    [InlineData("Warehouse")]
    [InlineData("Treasury")]
    public async Task Warehouse_reads_keep_own_branch_and_hide_other_branches(string role)
    {
        await using var f = await WorkflowFixture.CreateAsync(); f.Actor.Role = role;
        if (role == "Supervisor") f.Actor.UserId = "supervisor";
        var service = ActivatorUtilities.CreateInstance<WarehouseService>(f.Services);
        Assert.Single(await service.GetAllAsync());
        Assert.NotNull(await service.GetByIdAsync(1)); Assert.Null(await service.GetByIdAsync(2));
        var branches = ActivatorUtilities.CreateInstance<BranchService>(f.Services);
        Assert.Single(await branches.GetAllAsync());
        Assert.NotNull(await branches.GetBranchByIdAsync(1)); Assert.Null(await branches.GetBranchByIdAsync(2));
    }

    [Fact]
    public async Task Supervisor_cannot_move_or_create_customer_in_another_branch()
    {
        await using var f = await WorkflowFixture.CreateAsync(); f.Actor.Role = "Supervisor"; f.Actor.UserId = "supervisor";
        (await f.Db.Customers.FindAsync(1))!.BranchId = 1; await f.Db.SaveChangesAsync();
        var service = ActivatorUtilities.CreateInstance<CustomerService>(f.Services);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.CreateAsync(new CreateCustomerDto { Name = "Foreign", BranchId = 2 }));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(1, new UpdateCustomerDto { Id = 1, Name = "Foreign", BranchId = 2 }));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(1));
        Assert.Single(f.Db.Customers);
        Assert.Equal(1, (await f.Db.Customers.FindAsync(1))!.BranchId);
    }
    [Fact]
    public async Task Supervisor_can_still_create_and_update_customers_in_own_branch_without_a_route()
    {
        await using var f = await WorkflowFixture.CreateAsync(); f.Actor.Role = "Supervisor"; f.Actor.UserId = "supervisor";
        var service = ActivatorUtilities.CreateInstance<CustomerService>(f.Services);
        var created = await service.CreateAsync(new CreateCustomerDto { Name = "Local", Phone = "01012345678", Address = "Street" });
        Assert.Equal(1, created.BranchId);
        Assert.NotNull(await service.GetByIdAsync(created.Id));
        var updated = await service.UpdateAsync(created.Id, new UpdateCustomerDto { Id = created.Id, Name = "Renamed" });
        Assert.Equal("Renamed", updated.Name); Assert.Equal(1, updated.BranchId);
    }
    [Fact]
    public async Task Administration_customer_update_preserves_branch_when_omitted_as_existing_mapper_requires()
    {
        await using var f = await WorkflowFixture.CreateAsync(); f.Actor.Role = "Admin";
        (await f.Db.Customers.FindAsync(1))!.BranchId = 1; await f.Db.SaveChangesAsync();
        var service = ActivatorUtilities.CreateInstance<CustomerService>(f.Services);
        var result = await service.UpdateAsync(1, new UpdateCustomerDto { Id = 1, Name = "Updated", BranchId = null });
        Assert.Equal(1, result.BranchId);
        Assert.Equal("Updated", result.Name);
    }

}
