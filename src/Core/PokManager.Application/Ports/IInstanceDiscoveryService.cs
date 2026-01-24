using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for discovering and validating server instances.
/// Provides capabilities for instance enumeration and existence verification.
/// </summary>
public interface IInstanceDiscoveryService
{
    /// <summary>
    /// Discovers all available Palworld server instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of instance identifiers.</returns>
    Task<Result<IReadOnlyList<string>>> DiscoverInstancesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates any cached discovery information, forcing a fresh scan on the next discovery call.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the async operation.</returns>
    Task InvalidateCacheAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a specific instance exists.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the instance exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
