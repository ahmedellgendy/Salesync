using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Common.Licensing;
using Salesync.Application.Common.Settings;
using Salesync.Infrastructure.Modules.Identity.Entities;

namespace Salesync.Infrastructure.Seeds
{
    public static class InitialAdminSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            var settings =
                configuration
                    .GetSection("InitialAdmin")
                    .Get<InitialAdminSettings>();


            if (settings is null ||
                !settings.Enabled)
            {
                return;
            }


            if (string.IsNullOrWhiteSpace(settings.UserName) ||
                string.IsNullOrWhiteSpace(settings.Email) ||
                string.IsNullOrWhiteSpace(settings.FullName) ||
                string.IsNullOrWhiteSpace(settings.Password))
            {
                throw new InvalidOperationException(
                    "InitialAdmin configuration is incomplete.");
            }


            using var scope =
                serviceProvider.CreateScope();


            var userManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        UserManager<ApplicationUser>>();


            var roleManager =
                scope.ServiceProvider
                    .GetRequiredService<
                        RoleManager<ApplicationRole>>();


            var licenseService =
                scope.ServiceProvider
                    .GetRequiredService<ILicenseService>();


            const string adminRole =
                "Admin";


            // -------------------------------------------------
            // Ensure Admin role exists
            // -------------------------------------------------

            if (!await roleManager.RoleExistsAsync(
                    adminRole))
            {
                throw new InvalidOperationException(
                    "Admin role does not exist.");
            }


            // -------------------------------------------------
            // Do not create it twice
            // -------------------------------------------------

            var existingUser =
                await userManager.FindByNameAsync(
                    settings.UserName);


            if (existingUser is not null)
            {
                if (!await userManager.IsInRoleAsync(
                        existingUser,
                        adminRole))
                {
                    await userManager.AddToRoleAsync(
                        existingUser,
                        adminRole);
                }

                return;
            }


            var existingEmail =
                await userManager.FindByEmailAsync(
                    settings.Email);


            if (existingEmail is not null)
            {
                throw new InvalidOperationException(
                    $"Initial admin email '{settings.Email}' already belongs to another user.");
            }


            // -------------------------------------------------
            // License user limit
            // -------------------------------------------------

            var currentActiveUsers =
            await userManager.Users
                  .CountAsync(x => x.IsActive);


            licenseService.EnsureCanAddActiveUser(
                currentActiveUsers);


            // -------------------------------------------------
            // Create first admin
            // -------------------------------------------------

            var admin =
                new ApplicationUser
                {
                    UserName =
                        settings.UserName.Trim(),

                    Email =
                        settings.Email.Trim(),

                    FullName =
                        settings.FullName.Trim(),

                    IsActive =
                        true,

                    EmailConfirmed =
                        true,

                    CreatedAt =
                        DateTime.UtcNow
                };


            var createResult =
                await userManager.CreateAsync(
                    admin,
                    settings.Password);


            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join(
                        " | ",
                        createResult.Errors
                            .Select(x =>
                                x.Description)));
            }


            var roleResult =
                await userManager.AddToRoleAsync(
                    admin,
                    adminRole);


            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(admin);

                throw new InvalidOperationException(
                    string.Join(
                        " | ",
                        roleResult.Errors
                            .Select(x =>
                                x.Description)));
            }
        }
    }
}