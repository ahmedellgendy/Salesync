using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Salesync.Application.Common.Settings;
using Salesync.Application.Modules.Identity.Interfaces;
using Salesync.Infrastructure.Data;
using Salesync.Infrastructure.Modules.Identity.Entities;
using Salesync.Infrastructure.Modules.Identity.Services;
using System.Text;

namespace Salesync.Infrastructure.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            // configure jwtsettings 
            services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

            // Identity
            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // User settings
                options.User.RequireUniqueEmail = true;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
                .AddEntityFrameworkStores<SalesyncDbContext>();

            // Auth services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped(typeof(Func<IAuthService>), (serviceProvider) =>
            {
                return () => serviceProvider.GetService<IAuthService>();

            });

            // jwt Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = configuration["JwtSettings:Issuer"],
                        ValidAudience = configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Key"]!)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();
            return services;
        }
    }
}
