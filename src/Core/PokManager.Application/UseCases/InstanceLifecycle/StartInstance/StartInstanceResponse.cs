namespace PokManager.Application.UseCases.InstanceLifecycle.StartInstance;

/// <summary>
/// Response for a StartInstance operation.
/// </summary>
/// <param name="InstanceId">The identifier of the instance that was started.</param>
/// <param name="Message">Optional message providing additional context about the operation.</param>
public record StartInstanceResponse(
    string InstanceId,
    string? Message = null);
