using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for managing distributed locks to prevent concurrent operations on the same instance.
/// Ensures that only one operation can execute at a time for a given instance.
/// </summary>
public interface IOperationLockManager
{
    /// <summary>
    /// Attempts to acquire a lock for the specified instance and operation.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to lock.</param>
    /// <param name="operationId">The unique identifier of the operation requesting the lock.</param>
    /// <param name="timeout">Maximum time to wait for the lock to become available.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the acquired lock, which must be disposed to release the lock.</returns>
    Task<Result<IOperationLock>> AcquireLockAsync(
        string instanceId,
        string operationId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an instance is currently locked by any operation.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the instance is locked, false otherwise.</returns>
    Task<bool> IsLockedAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the operation ID that currently holds the lock for the specified instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The operation ID if locked, null if not locked.</returns>
    Task<string?> GetLockingOperationIdAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an acquired operation lock that prevents concurrent operations on an instance.
/// The lock is automatically released when disposed.
/// </summary>
public interface IOperationLock : IAsyncDisposable
{
    /// <summary>
    /// Gets the instance identifier that this lock is for.
    /// </summary>
    string InstanceId { get; }

    /// <summary>
    /// Gets the operation identifier that acquired this lock.
    /// </summary>
    string OperationId { get; }

    /// <summary>
    /// Gets the timestamp when this lock was acquired.
    /// </summary>
    DateTimeOffset AcquiredAt { get; }
}
