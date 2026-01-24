using Microsoft.AspNetCore.Mvc;
using PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;

namespace PokManager.ApiService.Endpoints;

/// <summary>
/// Endpoints for game configuration management (PalWorldSettings.ini)
/// </summary>
public static class GameConfigurationEndpoints
{
    public static void MapGameConfigurationEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/configuration").WithTags("Game Configuration");

        // GET /api/configuration/{instanceId} - Get game configuration
        group.MapGet("/{instanceId}", async (
            string instanceId,
            [FromQuery] bool includeSecrets,
            GetConfigurationHandler handler,
            CancellationToken ct) =>
        {
            var request = new GetConfigurationRequest(
                InstanceId: instanceId,
                IncludeSecrets: includeSecrets,
                CorrelationId: Guid.NewGuid().ToString()
            );

            var result = await handler.Handle(request, ct);

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("GetGameConfiguration")
        .WithOpenApi();

        // POST /api/configuration/{instanceId} - Apply game configuration
        group.MapPost("/{instanceId}", async (
            string instanceId,
            [FromBody] ApplyConfigurationRequestDto dto,
            ApplyConfigurationHandler handler,
            CancellationToken ct) =>
        {
            var request = new ApplyConfigurationRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                ConfigurationSettings: dto.Configuration,
                RestartInstance: dto.RestartInstance ?? false
            );

            var result = await handler.Handle(request, ct);

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            return Results.Ok(result.Value);
        })
        .WithName("ApplyGameConfiguration")
        .WithOpenApi();
    }

    private record ApplyConfigurationRequestDto(
        Dictionary<string, string> Configuration,
        bool? RestartInstance
    );
}
