using Autofac;
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

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<NutriBarnContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký service

builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
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

// Enable CORS BEFORE authorization and MapControllers
app.UseCors("AllowLocalhost");

app.UseAuthorization();

app.MapControllers();

app.Run();
