namespace PokManager.Application.UseCases.InstanceLifecycle.RecreateContainer;

/// <summary>
/// Request to recreate a Docker container (destroy + create).
/// Always preserves disk data - only recreates the container.
/// </summary>
/// <param name="InstanceId">The ID of the instance whose container should be recreated.</param>
/// <param name="CorrelationId">Correlation ID for tracking this operation.</param>
/// <param name="AutoStart">If true (default), starts the new container after creation.</param>
public record RecreateContainerRequest(
    string InstanceId,
    string CorrelationId,
    bool AutoStart = true
);
