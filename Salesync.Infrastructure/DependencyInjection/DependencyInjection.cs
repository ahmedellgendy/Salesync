using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Common.Licensing;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Interfaces.Services;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Application.Modules.Profile.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Infrastructure.Data;
using Salesync.Infrastructure.Licensing;
using Salesync.Infrastructure.Modules.Identity.Services;
using Salesync.Infrastructure.Repositories.Common;

namespace Salesync.Infrastructure.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services here

            // DbContext
            services.AddDbContext<SalesyncDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Register repositories
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            // register services for current user context
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IProfileService, ProfileService>();

            // license service
            services.AddScoped<ILicenseService, LicenseService>();

            services.AddSingleton<ILicenseSignatureVerifier, LicenseSignatureVerifier>();

            services.AddSingleton<ILicenseSignatureVerifier, LicenseSignatureVerifier>();

            services.AddScoped<ILicenseFileProvider, LicenseFileProvider>();

            services.AddScoped<ILicenseService, LicenseService>();

            return services;
        }
    }
}
