using PokManager.Application.BackgroundWorkers;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for a message queue that coordinates cache refresh requests.
/// </summary>
public interface IRefreshQueue
{
    /// <summary>
    /// Enqueues a single refresh request.
    /// </summary>
    Task EnqueueAsync(RefreshRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a single refresh request (blocks until one is available).
    /// </summary>
    Task<RefreshRequest?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enqueues multiple refresh requests in batch.
    /// </summary>
    Task EnqueueBatchAsync(IEnumerable<RefreshRequest> requests, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current depth of the queue.
    /// </summary>
    int GetQueueDepth();
}
