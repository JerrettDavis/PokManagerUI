using PokManager.Web;
using PokManager.Web.Components;
using PokManager.Web.Services;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceLifecycle.StartInstance;
using PokManager.Application.UseCases.InstanceLifecycle.StopInstance;
using PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Application.UseCases.InstanceManagement.SaveWorld;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;
using PokManager.Application.UseCases.InstanceDiscovery.ListInstances;
using PokManager.Application.UseCases.InstanceQuery;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Infrastructure.Fakes;
using PokManager.Infrastructure.Common;
using PokManager.Infrastructure.Docker.Services;
using MudBlazor.Services;
using Microsoft.EntityFrameworkCore;
using PokManager.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Enable detailed errors for debugging
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddMudServices();

// Add theme service
builder.Services.AddScoped<ThemeService>();

builder.Services.AddOutputCache();

// Get API service URL from configuration or use service discovery
var apiServiceUrl = builder.Configuration["Services:ApiService:Url"] ?? "https+http://apiservice";

builder.Services.AddHttpClient<WeatherApiClient>(client =>
    {
        // This URL uses "https+http://" to indicate HTTPS is preferred over HTTP.
        // Learn more about service discovery scheme resolution at https://aka.ms/dotnet/sdschemes.
        client.BaseAddress = new(apiServiceUrl);
    });

builder.Services.AddHttpClient<BackupApiClient>(client =>
    {
        client.BaseAddress = new(apiServiceUrl);
    });

builder.Services.AddHttpClient<LogApiClient>(client =>
    {
        client.BaseAddress = new(apiServiceUrl);
    });

builder.Services.AddHttpClient<ConfigurationTemplateApiClient>(client =>
    {
        client.BaseAddress = new(apiServiceUrl);
    });

builder.Services.AddHttpClient<PokManager.Web.Services.ServerService>(client =>
    {
        client.BaseAddress = new(apiServiceUrl);
    });

// HTTP Client for InstanceService to call API
builder.Services.AddHttpClient<InstanceService>(client =>
    {
        client.BaseAddress = new(apiServiceUrl);
    });

// Add SQLite database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "pokmanager.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
builder.Services.AddDbContextFactory<PokManagerDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Add repository
builder.Services.AddScoped<InstanceDataRepository>();

// Add RCON service
builder.Services.AddScoped<RconService>();

// Add instance data cache and worker
builder.Services.AddSingleton<InstanceDataCache>();
builder.Services.AddHostedService<InstanceDataWorker>();

// Register infrastructure services for configuration management
builder.Services.AddSingleton<IDockerService, LocalDockerService>();
builder.Services.AddScoped<IPokManagerClient, DockerPokManagerClient>();
builder.Services.AddScoped<IOperationLockManager, InMemoryOperationLockManager>();
builder.Services.AddScoped<IAuditSink, InMemoryAuditSink>();
builder.Services.AddScoped<IClock, SystemClock>();

// Register configuration handlers and service
builder.Services.AddScoped<GetConfigurationHandler>();
builder.Services.AddScoped<ApplyConfigurationHandler>();
builder.Services.AddScoped<ConfigurationService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PokManagerDbContext>();
    db.Database.Migrate();

    // Seed preset configuration templates
    await PresetTemplateSeeder.SeedPresetsAsync(db);
}

app.Run();
