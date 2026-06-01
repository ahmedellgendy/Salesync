using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Modules.MasterData.Validators.Customer;

namespace Salesync.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Register application services here
             services.AddValidatorsFromAssemblyContaining<CustomerCreateValidator>();
             services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

            return services;
        }
    }
}
