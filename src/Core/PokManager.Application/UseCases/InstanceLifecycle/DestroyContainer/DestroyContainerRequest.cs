namespace PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;

/// <summary>
/// Request to destroy (stop and remove) a Docker container for an instance.
/// </summary>
/// <param name="InstanceId">The ID of the instance whose container should be destroyed.</param>
/// <param name="CorrelationId">Correlation ID for tracking this operation.</param>
/// <param name="PreserveData">If true (default), preserves disk data and volumes. If false, removes volumes.</param>
public record DestroyContainerRequest(
    string InstanceId,
    string CorrelationId,
    bool PreserveData = true
);
