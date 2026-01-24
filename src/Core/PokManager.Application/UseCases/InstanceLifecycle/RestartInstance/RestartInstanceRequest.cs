namespace PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;

/// <summary>
/// Request to restart a Palworld server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier of the instance to restart.</param>
/// <param name="CorrelationId">A unique identifier to track this operation across systems.</param>
/// <param name="GracePeriodSeconds">The grace period in seconds before forcefully restarting (default: 30).</param>
/// <param name="SaveWorld">Whether to save the world before restarting (default: true).</param>
/// <param name="WaitForHealthy">Whether to wait for the instance to become healthy after restart (default: true).</param>
public record RestartInstanceRequest(
    string InstanceId,
    string CorrelationId,
    int GracePeriodSeconds = 30,
    bool SaveWorld = true,
    bool WaitForHealthy = true);
