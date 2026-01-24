using PokManager.Application.UseCases.InstanceDiscovery.ListInstances;
using PokManager.Application.UseCases.InstanceQuery;
using PokManager.Application.UseCases.InstanceLifecycle.StartInstance;
using PokManager.Application.UseCases.InstanceLifecycle.StopInstance;
using PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;
using PokManager.Application.Caching;
using PokManager.Application.Ports;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Configuration;

namespace PokManager.ApiService.Endpoints;

public static class InstanceEndpoints
{
    public static void MapInstanceEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/instances").WithTags("Instances");

        // GET /api/instances - List all instances
        group.MapGet("/", async (
            ListInstancesHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(new ListInstancesRequest(), ct);
            
            return result.IsSuccess 
                ? Results.Ok(result.Value) 
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ListInstances")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/status - Get instance status (cache-first)
        group.MapGet("/{instanceId}/status", async (
            string instanceId,
            ICacheService cacheService,
            GetInstanceStatusHandler handler,
            CacheConfiguration cacheConfig,
            CancellationToken ct) =>
        {
            // Try cache first
            var cached = await cacheService.GetAsync<GetInstanceStatusResponse>(
                CacheKeys.InstanceStatus(instanceId),
                ct);

            if (cached != null)
            {
                return Results.Ok(cached);
            }

            // Cache miss - fetch from handler and cache
            var result = await handler.Handle(
                new GetInstanceStatusRequest(instanceId, Guid.NewGuid().ToString()),
                ct);

            if (result.IsFailure)
            {
                return Results.NotFound(new { error = result.Error });
            }

            // Cache the result
            await cacheService.SetAsync(
                CacheKeys.InstanceStatus(instanceId),
                result.Value,
                cacheConfig.InstanceStatusTtl,
                ct);

            return Results.Ok(result.Value);
        })
        .WithName("GetInstanceStatus")
        .WithOpenApi();

        // POST /api/instances/{instanceId}/status/refresh - Trigger priority refresh of instance status
        group.MapPost("/{instanceId}/status/refresh", async (
            string instanceId,
            IRefreshQueue refreshQueue,
            CancellationToken ct) =>
        {
            await refreshQueue.EnqueueAsync(
                new RefreshRequest(RefreshType.InstanceStatus, instanceId, Priority: true),
                ct);

            return Results.Accepted(null, new { message = $"Instance status refresh queued for {instanceId}" });
        })
        .WithName("RefreshInstanceStatus")
        .WithOpenApi();

        // POST /api/instances/{instanceId}/start - Start an instance
        group.MapPost("/{instanceId}/start", async (
            string instanceId,
            StartInstanceHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new StartInstanceRequest(instanceId, Guid.NewGuid().ToString()), 
                ct);
            
            return result.IsSuccess 
                ? Results.Ok(new { message = result.Value.Message }) 
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("StartInstance")
        .WithOpenApi();

        // POST /api/instances/{instanceId}/stop - Stop an instance
        group.MapPost("/{instanceId}/stop", async (
            string instanceId,
            StopInstanceHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new StopInstanceRequest(instanceId, Guid.NewGuid().ToString(), ForceKill: false), 
                ct);
            
            return result.IsSuccess 
                ? Results.Ok(new { message = result.Value.Message }) 
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("StopInstance")
        .WithOpenApi();

        // POST /api/instances/{instanceId}/restart - Restart an instance
        group.MapPost("/{instanceId}/restart", async (
            string instanceId,
            RestartInstanceHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new RestartInstanceRequest(instanceId, Guid.NewGuid().ToString(), GracePeriodSeconds: 30), 
                ct);
            
            return result.IsSuccess 
                ? Results.Ok(new { message = result.Value.Message }) 
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("RestartInstance")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs - Get Docker container logs
        group.MapGet("/{instanceId}/logs", async (
            string instanceId,
            PokManager.Infrastructure.Docker.Services.IDockerService dockerService,
            int tail = 100,
            CancellationToken ct = default) =>
        {
            try
            {
                // Get container ID for the instance
                var containers = await dockerService.ListContainersAsync(ct);
                var container = containers.FirstOrDefault(c => c.Name.Contains(instanceId, StringComparison.OrdinalIgnoreCase));
                
                if (container == null)
                {
                    return Results.NotFound(new { error = $"Container for instance '{instanceId}' not found" });
                }

                // Fetch logs from Docker
                var logs = await dockerService.GetContainerLogsAsync(container.Id, tail, ct);
                
                return Results.Ok(new { 
                    instanceId = instanceId,
                    containerId = container.Id,
                    logs = logs
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error fetching container logs"
                );
            }
        })
        .WithName("GetInstanceLogs")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/config - Get docker-compose configuration
        group.MapGet("/{instanceId}/config", (
            string instanceId) =>
        {
            try
            {
                // Path to docker-compose files on the server
                var basePath = "/home/pokuser/asa_server";
                var configPath = Path.Combine(basePath, $"Instance_{instanceId}", $"docker-compose-{instanceId}.yaml");
                
                if (!File.Exists(configPath))
                {
                    return Results.NotFound(new { error = $"Configuration file not found for instance '{instanceId}'" });
                }

                var parser = new PokManager.Infrastructure.Docker.Services.DockerComposeParser();
                var config = parser.ParseFile(configPath);
                
                if (config == null)
                {
                    return Results.Problem(
                        detail: "Failed to parse docker-compose configuration",
                        statusCode: 500
                    );
                }

                return Results.Ok(config);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Error reading instance configuration"
                );
            }
        })
        .WithName("GetInstanceConfig")
        .WithOpenApi();
    }
}
