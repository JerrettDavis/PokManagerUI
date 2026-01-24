namespace PokManager.Application.Ports;

/// <summary>
/// Interface for cache invalidation operations with automatic refresh triggering.
/// </summary>
public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidates all cache entries for a specific instance and triggers immediate refresh.
    /// </summary>
    Task InvalidateInstanceAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates backup-related cache entries for a specific instance and triggers immediate refresh.
    /// </summary>
    Task InvalidateBackupsAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates configuration cache for a specific instance and triggers immediate refresh.
    /// </summary>
    Task InvalidateConfigurationAsync(string instanceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all instance-related cache entries (global invalidation).
    /// </summary>
    Task InvalidateAllInstancesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates template-related cache entries and triggers immediate refresh.
    /// </summary>
    Task InvalidateTemplatesAsync(CancellationToken cancellationToken = default);
}
