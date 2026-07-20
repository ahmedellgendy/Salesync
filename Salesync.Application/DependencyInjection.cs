using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Salesync.Application.Modules.CustomerVisit.Interfaces;
using Salesync.Application.Modules.CustomerVisit.Services;
using Salesync.Application.Modules.Inventory.Interfaces;
using Salesync.Application.Modules.Inventory.Services;
using Salesync.Application.Modules.LoadRequest.Interfaces;
using Salesync.Application.Modules.LoadRequest.Services;
using Salesync.Application.Modules.MasterData.Validators.Customer;
using Salesync.Application.Modules.Sales.Interfaces;
using Salesync.Application.Modules.Sales.Services;
using Salesync.Application.Modules.SalesRep.Interfaces.Services;
using Salesync.Application.Modules.SalesRep.Services;
using Salesync.Application.Modules.Treasury.Interfaces.Services;
using Salesync.Application.Modules.Treasury.Services;

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
            services.AddScoped<ISalesRepAccountService,SalesRepAccountService>();

            services.AddScoped<ISalesRepMobileService, SalesRepMobileService>();


            // Register Sales Service
            services.AddScoped<ISalesRepSessionService, SalesRepSessionService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<IInvoiceReturnService, InvoiceReturnService>();
            services.AddScoped<ISalesRepDayClosingCalculator, SalesRepDayClosingCalculator>();
            services.AddScoped<ISalesRepDayClosingSettlementService, SalesRepDayClosingSettlementService>();
            services.AddScoped<ISalesRepDayClosingService, SalesRepDayClosingService>();

            // Register Inventory Service
            services.AddScoped<IInventoryService, InventoryService>();

            // Register LoadRequest Service
            services.AddScoped<ILoadRequestService, LoadRequestService>();

            // Register CustomerVisit Service
            services.AddScoped<ICustomerVisitService, CustomerVisitService>();

            // Register Treasury Service
            services.AddScoped<ITreasuryService, TreasuryService>();


            return services;
        }
    }
}
