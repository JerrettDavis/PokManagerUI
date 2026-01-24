namespace PokManager.Application.Models;

/// <summary>
/// Options for restarting a Palworld server instance.
/// </summary>
/// <param name="Graceful">Whether to perform a graceful shutdown before restarting.</param>
/// <param name="SaveWorld">Whether to save the world before restarting.</param>
/// <param name="WaitForHealthy">Whether to wait for the instance to become healthy after restart.</param>
/// <param name="Timeout">Maximum time to wait for the restart operation to complete.</param>
public record RestartInstanceOptions(
    bool Graceful = true,
    bool SaveWorld = true,
    bool WaitForHealthy = true,
    TimeSpan? Timeout = null
);
