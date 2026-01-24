namespace PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;

/// <summary>
/// Response from restarting a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance that was restarted.</param>
/// <param name="Message">A message describing the result of the operation.</param>
/// <param name="RestartedAt">The timestamp when the restart operation completed.</param>
public record RestartInstanceResponse(
    string InstanceId,
    string Message,
    DateTimeOffset RestartedAt);
