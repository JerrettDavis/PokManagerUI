using Microsoft.AspNetCore.Mvc;
using PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;
using PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;
using PokManager.Application.UseCases.InstanceLifecycle.RecreateContainer;

namespace PokManager.ApiService.Endpoints;

/// <summary>
/// Endpoints for managing Docker container lifecycle operations.
/// </summary>
public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/instances/{instanceId}/container")
            .WithTags("Container Lifecycle")
            .WithOpenApi();

        // POST /api/instances/{instanceId}/container/create
        group.MapPost("/create", async (
            [FromRoute] string instanceId,
            [FromQuery] bool autoStart,
            [FromServices] CreateContainerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var request = new CreateContainerRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                AutoStart: autoStart);

            var result = await handler.Handle(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateContainer")
        .WithOpenApi(op =>
        {
            op.Summary = "Create a Docker container for an existing instance";
            op.Description = "Creates and optionally starts a Docker container from the instance's docker-compose file. " +
                           "Useful for instances that have disk data but no running container (e.g., after manual cleanup or migration).";
            return op;
        });

        // POST /api/instances/{instanceId}/container/destroy
        group.MapPost("/destroy", async (
            [FromRoute] string instanceId,
            [FromQuery] bool preserveData,
            [FromServices] DestroyContainerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var request = new DestroyContainerRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                PreserveData: preserveData);

            var result = await handler.Handle(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("DestroyContainer")
        .WithOpenApi(op =>
        {
            op.Summary = "Destroy (stop and remove) a Docker container";
            op.Description = "Stops and removes the Docker container for an instance. " +
                           "By default (preserveData=true), all disk data and volumes are preserved. " +
                           "Set preserveData=false to also remove Docker volumes (DESTRUCTIVE).";
            return op;
        });

        // POST /api/instances/{instanceId}/container/recreate
        group.MapPost("/recreate", async (
            [FromRoute] string instanceId,
            [FromQuery] bool autoStart,
            [FromServices] RecreateContainerHandler handler,
            CancellationToken cancellationToken) =>
        {
            var request = new RecreateContainerRequest(
                InstanceId: instanceId,
                CorrelationId: Guid.NewGuid().ToString(),
                AutoStart: autoStart);

            var result = await handler.Handle(request, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("RecreateContainer")
        .WithOpenApi(op =>
        {
            op.Summary = "Recreate a Docker container (destroy + create)";
            op.Description = "Destroys the existing container (preserving data) and creates a new one. " +
                           "Useful for refreshing container configuration or recovering from container issues. " +
                           "All disk data is always preserved.";
            return op;
        });
    }
}
