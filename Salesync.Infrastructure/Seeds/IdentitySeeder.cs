using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Seeds
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {

            using var scope = serviceProvider.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            var roles = new[]
            {
                "Admin",
                "Management",
                "Supervisor",
                "SalesRep",
                "Warehouse",
                "User",
                "Treasury"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role });
                }
            }

        }

        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var adminUser = await userManager.FindByNameAsync("ahmed.elgendy");
            if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        public static async Task SeedSupervisorUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var supervisor = await userManager.FindByNameAsync("supervisor.test");

            if (supervisor is null)
            {
                supervisor = await userManager.FindByEmailAsync("supervisor@salesync.local");
            }

            if (supervisor is null)
            {
                supervisor = new ApplicationUser
                {
                    UserName = "supervisor.test",
                    Email = "supervisor@salesync.local",
                    FullName = "Test Supervisor",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(supervisor, "Supervisor@12345");

                if (!createResult.Succeeded)
                {
                    throw new Exception(string.Join(" | ", createResult.Errors.Select(x => x.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(supervisor, "Supervisor"))
            {
                var roleResult = await userManager.AddToRoleAsync(supervisor, "Supervisor");

                if (!roleResult.Succeeded)
                {
                    throw new Exception(string.Join(" | ", roleResult.Errors.Select(x => x.Description)));
                }
            }
        }

        public static async Task SeedManagementUserAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

            var management = await userManager
                .FindByNameAsync("management.test");

            if (management is null)
            {
                management = await userManager
                    .FindByEmailAsync("management@salesync.local");
            }

            if (management is null)
            {
                management = new ApplicationUser
                {
                    UserName = "management.test",
                    Email = "management@salesync.local",
                    FullName = "Test Management",
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(
                    management,
                    "Management@12345");

                if (!createResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            " | ",
                            createResult.Errors.Select(x => x.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(
                    management,
                    "Management"))
            {
                var roleResult = await userManager.AddToRoleAsync(
                    management,
                    "Management");

                if (!roleResult.Succeeded)
                {
                    throw new Exception(
                        string.Join(
                            " | ",
                            roleResult.Errors.Select(x => x.Description)));
                }
            }
        }
    }
}
