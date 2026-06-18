using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Infrastructure.Data;
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


            return services;
        }
    }
}
