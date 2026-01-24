namespace PokManager.Application.UseCases.InstanceManagement.SaveWorld;

/// <summary>
/// Response indicating successful world save operation.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance that was saved.</param>
/// <param name="SavedAt">The timestamp when the save operation was completed.</param>
/// <param name="Message">Optional message about the save operation.</param>
public record SaveWorldResponse(
    string InstanceId,
    DateTimeOffset SavedAt,
    string? Message = null);
