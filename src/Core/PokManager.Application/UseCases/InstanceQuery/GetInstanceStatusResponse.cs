namespace PokManager.Application.UseCases.InstanceQuery;

/// <summary>
/// Response containing the status of a specific instance.
/// </summary>
/// <param name="Status">The instance status information.</param>
public record GetInstanceStatusResponse(
    InstanceStatusDto Status);
