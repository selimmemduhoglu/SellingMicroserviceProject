using CatalogService.Api.Extensions;
using CatalogService.Api.Infrastructure;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;

string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configuration setup for appsettings.json and environment-specific settings
IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"Configurations/appsettings.json", optional: false)
    .AddJsonFile($"Configurations/appsettings.{env}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

builder.Configuration.AddConfiguration(configuration);

// Configuration setup for serilog.json and environment-specific settings
IConfigurationRoot serilogConfiguration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile($"Configurations/serilog.json", optional: false)
    .AddJsonFile($"Configurations/serilog.{env}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(serilogConfiguration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CatalogService.Api", Version = "v1" });
});
builder.Services.Configure<CatalogSettings>(builder.Configuration.GetSection("CatalogSettings"));

// Add DbContext configuration
builder.Services.ConfigureDbContext(builder.Configuration);

// Add Consul configuration
builder.Services.ConfigureConsul(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "CatalogService.Api v1"));
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions()
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "Pics")),
    RequestPath = "/pics"
});

app.UseAuthorization();

app.MapControllers();

app.RegisterWithConsul(app.Lifetime, builder.Configuration);

app.Run();
