using Microsoft.AspNetCore.Mvc;
using PokManager.Infrastructure.Docker.Models;
using PokManager.Infrastructure.Docker.Services;

namespace PokManager.ApiService.Endpoints;

public static class ServerEndpoints
{
    public static void MapServerEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/servers").WithTags("Servers");

        group.MapGet("/", async (IDockerService dockerService, CancellationToken ct) =>
        {
            var containers = await dockerService.ListContainersAsync(ct);
            return Results.Ok(containers);
        })
        .WithName("GetServers")
        .WithOpenApi();

        group.MapGet("/{nameOrId}", async (string nameOrId, IDockerService dockerService, CancellationToken ct) =>
        {
            var container = await dockerService.GetContainerAsync(nameOrId, ct);
            return container is not null ? Results.Ok(container) : Results.NotFound();
        })
        .WithName("GetServer")
        .WithOpenApi();

        group.MapPost("/{nameOrId}/start", async (string nameOrId, IDockerService dockerService, CancellationToken ct) =>
        {
            var success = await dockerService.StartContainerAsync(nameOrId, ct);
            return success ? Results.Ok(new { message = "Server started successfully" }) : Results.BadRequest();
        })
        .WithName("StartServer")
        .WithOpenApi();

        group.MapPost("/{nameOrId}/stop", async (string nameOrId, IDockerService dockerService, CancellationToken ct) =>
        {
            var success = await dockerService.StopContainerAsync(nameOrId, ct);
            return success ? Results.Ok(new { message = "Server stopped successfully" }) : Results.BadRequest();
        })
        .WithName("StopServer")
        .WithOpenApi();

        group.MapPost("/{nameOrId}/restart", async (string nameOrId, IDockerService dockerService, CancellationToken ct) =>
        {
            var success = await dockerService.RestartContainerAsync(nameOrId, ct);
            return success ? Results.Ok(new { message = "Server restarted successfully" }) : Results.BadRequest();
        })
        .WithName("RestartServer")
        .WithOpenApi();

        group.MapGet("/{nameOrId}/logs", async (string nameOrId, [FromQuery] int lines, IDockerService dockerService, CancellationToken ct) =>
        {
            var logs = await dockerService.GetContainerLogsAsync(nameOrId, lines > 0 ? lines : 100, ct);
            return Results.Ok(new { logs });
        })
        .WithName("GetServerLogs")
        .WithOpenApi();
    }
}
