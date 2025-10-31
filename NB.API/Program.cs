using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using NB.API.Modules;
using NB.Model.Entities;
using NB.Service.Core.Mapper;

var builder = WebApplication.CreateBuilder(args);

//  Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000") // frontend origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NutriBarnContext>(options =>
{

    options.UseSqlServer("Server=localhost;Database=NutriBarn;User Id=sa;Password=123456;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
           .EnableSensitiveDataLogging()  // Hiển thị giá trị parameters
           .LogTo(Console.WriteLine, LogLevel.Information); // Log ra console
});

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
