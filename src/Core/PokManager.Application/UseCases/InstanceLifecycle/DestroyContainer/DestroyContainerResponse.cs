namespace PokManager.Application.UseCases.InstanceLifecycle.DestroyContainer;

/// <summary>
/// Response from destroying a Docker container.
/// </summary>
/// <param name="InstanceId">The ID of the instance.</param>
/// <param name="Success">Indicates if the container was destroyed successfully.</param>
/// <param name="DataPreserved">Indicates if disk data was preserved.</param>
/// <param name="Message">Additional information about the operation.</param>
public record DestroyContainerResponse(
    string InstanceId,
    bool Success,
    bool DataPreserved,
    string? Message = null
);
