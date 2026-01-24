using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Interface for backup storage operations.
/// Provides access to backup files and metadata, supporting retrieval and cleanup operations.
/// </summary>
public interface IBackupStore
{
    /// <summary>
    /// Lists all backups for a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of backup information.</returns>
    Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a stream to read a specific backup file.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="backupId">The unique identifier of the backup.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a stream of the backup file data.</returns>
    Task<Result<Stream>> GetBackupStreamAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the oldest backups for an instance, keeping only the specified number of most recent backups.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="keepCount">The number of most recent backups to keep.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> DeleteOldestBackupsAsync(
        string instanceId,
        int keepCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the total size of all backups for a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the total size in bytes.</returns>
    Task<Result<long>> GetTotalBackupSizeAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores metadata about a newly created backup in the backup store.
    /// </summary>
    /// <param name="backupInfo">The backup information to store.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> StoreBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default);
}
