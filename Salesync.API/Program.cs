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
using Salesync.Infrastructure.Repositories.Common;


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


builder.Services.AddValidatorsFromAssemblyContaining<CustomerCreateValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

// Exception Handler
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



// Database Connection
builder.Services.AddDbContext<SalesyncDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


// Services register
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();


// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));


var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Salesync API v1"));
}


app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();