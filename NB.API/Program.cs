using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NB.API.Modules;
using NB.Model.Entities;
using NB.Repository.Common;
using NB.Service.EmployeeService;
using NB.Service.Core.Mapper;
using NB.Service.WarehouseService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<NutriBarnContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký service
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

//builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>

// Replace the following line:  
// builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());  

//{
//    //containerBuilder.RegisterModule<EFModule>();
//    containerBuilder.RegisterModule<RepositoryModule>();
//    containerBuilder.RegisterModule<ServiceModule>();
//});


builder.Services.AddDbContext<NutriBarnContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<DbContext, NutriBarnContext>();

//Register AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddScoped<IMapper, Mapper>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
