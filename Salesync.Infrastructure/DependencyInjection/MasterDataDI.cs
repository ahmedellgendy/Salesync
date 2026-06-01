using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Modules.MasterData.Interfaces.Services;
using Salesync.Application.Modules.MasterData.Services;
using Salesync.Infrastructure.Data;
using Salesync.Infrastructure.Repositories.Common;

namespace Salesync.Infrastructure.DependencyInjection
{
    public static class MasterDataDI
    {
        public static IServiceCollection AddMasterDataModule(this IServiceCollection services, IConfiguration configuration)
        {
            // Register services for MasterData module here
            services.AddScoped<IBranchService, BranchService>();
            services.AddScoped<IWarehouseService, WarehouseService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICustomerService, CustomerService>();

            return services;
        }
    }
}
