using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Modules.MasterData.Validators.Customer;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;

namespace Salesync.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services here
            services.AddValidatorsFromAssemblyContaining<CustomerCreateValidator>();
            services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();
      
            // Register SalesRep Service
            services.AddScoped<ISalesRepService, SalesRepService>();
            services.AddScoped<IRouteService, RouteService>();
            services.AddScoped<IRouteCustomerService, RouteCustomerService>();

            return services;
        }
    }
}
