using PokManager.ApiService.Endpoints;
using PokManager.Infrastructure.Docker.Services;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceDiscovery.ListInstances;
using PokManager.Application.UseCases.InstanceQuery;
using PokManager.Application.UseCases.InstanceLifecycle.StartInstance;
using PokManager.Application.UseCases.InstanceLifecycle.StopInstance;
using PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;
using PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;
using PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;
using PokManager.Application.UseCases.InstanceLifecycle.RecreateContainer;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Application.UseCases.BackupManagement.RestoreBackup;
using PokManager.Application.UseCases.BackupManagement.UploadBackup;
using PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;
using PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.DeleteTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ExportTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ImportTemplate;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Infrastructure.Common;
using PokManager.Infrastructure.Fakes;
using PokManager.Infrastructure.Caching;
using PokManager.Infrastructure.BackgroundWorkers;
using PokManager.Application.Configuration;
using PokManager.Web.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register Docker service - use LocalDockerService for production deployment
builder.Services.AddSingleton<IDockerService, LocalDockerService>();

// Register Docker-based implementations
// Use DiskBasedInstanceDiscoveryService for disk-first discovery
builder.Services.AddScoped<IInstanceDiscoveryService>(sp =>
{
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DiskBasedInstanceDiscoveryService>>();
    return new DiskBasedInstanceDiscoveryService(
        logger,
        basePath: "/home/pokuser/asa_server",
        cacheLifetime: TimeSpan.FromMinutes(5));
});
builder.Services.AddScoped<IPokManagerClient, DockerPokManagerClient>();

// Register Docker Compose service for container lifecycle management
builder.Services.AddScoped<IDockerComposeService, LocalDockerComposeService>();

// Register infrastructure services
builder.Services.AddScoped<IOperationLockManager, InMemoryOperationLockManager>();
builder.Services.AddScoped<IAuditSink, InMemoryAuditSink>();
builder.Services.AddScoped<IClock, SystemClock>();
builder.Services.AddSingleton<IBackupStore, InMemoryBackupStore>();

// Add SQLite database for configuration templates
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "pokmanager.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContext<PokManagerDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<IConfigurationTemplateStore, ConfigurationTemplateStore>();

// Register caching infrastructure
var cacheConfig = new CacheConfiguration();
builder.Configuration.GetSection("Cache").Bind(cacheConfig);
builder.Services.AddSingleton(cacheConfig);
builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
builder.Services.AddSingleton<IRefreshQueue, RefreshQueue>();
builder.Services.AddSingleton<ICacheInvalidationService, CacheInvalidationService>();

// Register background workers
builder.Services.AddHostedService<ScheduledRefreshWorker>();
builder.Services.AddHostedService<InstanceStatusRefreshWorker>();
builder.Services.AddHostedService<BackupListRefreshWorker>();

// Register use-case handlers
builder.Services.AddScoped<ListInstancesHandler>();
builder.Services.AddScoped<GetInstanceStatusHandler>();
builder.Services.AddScoped<StartInstanceHandler>();
builder.Services.AddScoped<StopInstanceHandler>();
builder.Services.AddScoped<RestartInstanceHandler>();
builder.Services.AddScoped<ListBackupsHandler>();
builder.Services.AddScoped<CreateBackupHandler>();
builder.Services.AddScoped<RestoreBackupHandler>();
builder.Services.AddScoped<UploadBackupHandler>();
builder.Services.AddScoped<GetConfigurationHandler>();
builder.Services.AddScoped<PokManager.Application.UseCases.Configuration.ApplyConfiguration.ApplyConfigurationHandler>();

// Register configuration template handlers
builder.Services.AddScoped<ListTemplatesHandler>();
builder.Services.AddScoped<SaveTemplateHandler>();
builder.Services.AddScoped<PreviewTemplateHandler>();
builder.Services.AddScoped<ApplyTemplateHandler>();
builder.Services.AddScoped<DeleteTemplateHandler>();
builder.Services.AddScoped<ExportTemplateHandler>();
builder.Services.AddScoped<ImportTemplateHandler>();

// Register container lifecycle handlers
builder.Services.AddScoped<CreateContainerHandler>();
builder.Services.AddScoped<DestroyContainerHandler>();
builder.Services.AddScoped<RecreateContainerHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Map server management endpoints
app.MapServerEndpoints();

// Map instance management endpoints
app.MapInstanceEndpoints();

// Map container lifecycle endpoints
app.MapContainerEndpoints();

// Map backup management endpoints
app.MapBackupEndpoints();

// Map configuration template endpoints
app.MapConfigurationTemplateEndpoints();

// Map log endpoints
app.MapLogEndpoints();

// Map cache endpoints
app.MapCacheEndpoints();

// Map game configuration endpoints
app.MapGameConfigurationEndpoints();

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
