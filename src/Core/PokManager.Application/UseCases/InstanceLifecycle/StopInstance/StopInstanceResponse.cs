namespace PokManager.Application.UseCases.InstanceLifecycle.StopInstance;

/// <summary>
/// Response for a StopInstance operation.
/// </summary>
/// <param name="InstanceId">The identifier of the instance that was stopped.</param>
/// <param name="Message">Optional message providing additional context about the operation.</param>
public record StopInstanceResponse(
    string InstanceId,
    string? Message = null);
