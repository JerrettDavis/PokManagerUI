namespace PokManager.Application.UseCases.InstanceLifecycle.CreateContainer;

/// <summary>
/// Response from creating a Docker container.
/// </summary>
/// <param name="InstanceId">The ID of the instance.</param>
/// <param name="ContainerId">The Docker container ID, if available.</param>
/// <param name="Success">Indicates if the container was created successfully.</param>
/// <param name="Message">Additional information about the operation.</param>
public record CreateContainerResponse(
    string InstanceId,
    string? ContainerId,
    bool Success,
    string? Message = null
);
