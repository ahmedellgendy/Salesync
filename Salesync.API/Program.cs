using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Salesync.API.Middleware;
using Salesync.Application;
using Salesync.Application.Interfaces.Repositories;
using Salesync.Application.Mappings;
using Salesync.Application.Modules.MasterData.Interfaces.Services;
using Salesync.Application.Modules.MasterData.Services;
using Salesync.Application.Modules.MasterData.Validators.Customer;
using Salesync.Infrastructure.Data;
using Salesync.Infrastructure.DependencyInjection;
using Salesync.Infrastructure.Extensions;
using Salesync.Infrastructure.Repositories.Common;
using Salesync.Infrastructure.Seeds;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            return new BadRequestObjectResult(context.ModelState);
        };
    });

builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddMasterDataModule(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Salesync API",
        Version = "v1"
    });
});

var app = builder.Build();

// Seed roles
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);
    await IdentitySeeder.SeedAdminUserAsync(scope.ServiceProvider);
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Salesync API v1"));
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();