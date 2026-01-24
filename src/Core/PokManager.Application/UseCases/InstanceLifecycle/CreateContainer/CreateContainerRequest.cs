namespace PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;

/// <summary>
/// Request to create a Docker container for an existing instance.
/// </summary>
/// <param name="InstanceId">The ID of the instance to create a container for.</param>
/// <param name="CorrelationId">Correlation ID for tracking this operation.</param>
/// <param name="AutoStart">If true, starts the container after creation. Default is true.</param>
public record CreateContainerRequest(
    string InstanceId,
    string CorrelationId,
    bool AutoStart = true
);
