namespace PokManager.Application.UseCases.InstanceLifecycle.RecreateContainer;

/// <summary>
/// Response from recreating a Docker container.
/// </summary>
/// <param name="InstanceId">The ID of the instance.</param>
/// <param name="Success">Indicates if the container was recreated successfully.</param>
/// <param name="Message">Additional information about the operation.</param>
public record RecreateContainerResponse(
    string InstanceId,
    bool Success,
    string? Message = null
);
