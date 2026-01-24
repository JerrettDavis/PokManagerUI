namespace PokManager.Application.Models;

/// <summary>
/// Options for stopping a Palworld server instance.
/// </summary>
/// <param name="Graceful">Whether to perform a graceful shutdown (save world before stopping).</param>
/// <param name="Timeout">Maximum time to wait for graceful shutdown before forcing stop.</param>
/// <param name="SaveWorld">Whether to explicitly save the world before stopping.</param>
public record StopInstanceOptions(
    bool Graceful = true,
    TimeSpan? Timeout = null,
    bool SaveWorld = true
);
