using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NB.API.Modules;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.CategoryService;
using NB.Service.Core.Mapper;
using NB.Service.InventoryService;
using NB.Service.ProductService;
using NB.Service.UserService;
using NB.Service.RoleService;
using NB.Service.UserRoleService;
using NB.Service.SupplierService;
using NB.Service.WarehouseService;

var builder = WebApplication.CreateBuilder(args);

//  Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy
                .WithOrigins("https://localhost:3000") // frontend origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<EFModule>();
    containerBuilder.RegisterModule<RepositoryModule>();
    containerBuilder.RegisterModule<ServiceModule>();
});

//add DI
builder.Services.AddScoped<IMapper, Mapper>();


builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS BEFORE authorization and MapControllers
app.UseCors("AllowLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();
