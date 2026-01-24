using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Application.Ports;

/// <summary>
/// Primary interface for managing Palworld server instances.
/// This port defines the contract that infrastructure implementations must fulfill
/// for all instance lifecycle, backup, update, and configuration operations.
/// </summary>
public interface IPokManagerClient
{
    #region Discovery & Query

    /// <summary>
    /// Lists all available Palworld server instances.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of instance identifiers.</returns>
    Task<Result<IReadOnlyList<string>>> ListInstancesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the instance status information.</returns>
    Task<Result<InstanceStatus>> GetInstanceStatusAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing detailed instance information.</returns>
    Task<Result<InstanceDetails>> GetInstanceDetailsAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Lifecycle Management

    /// <summary>
    /// Creates a new Palworld server instance.
    /// </summary>
    /// <param name="request">The instance creation request containing configuration details.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the created instance identifier.</returns>
    Task<Result<string>> CreateInstanceAsync(
        CreateInstanceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a stopped Palworld server instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to start.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> StartInstanceAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a running Palworld server instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to stop.</param>
    /// <param name="options">Options controlling the stop behavior.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> StopInstanceAsync(
        string instanceId,
        StopInstanceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restarts a Palworld server instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to restart.</param>
    /// <param name="options">Options controlling the restart behavior.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> RestartInstanceAsync(
        string instanceId,
        RestartInstanceOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a Palworld server instance and all its associated data.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to delete.</param>
    /// <param name="deleteBackups">Whether to also delete all backups for this instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> DeleteInstanceAsync(
        string instanceId,
        bool deleteBackups = false,
        CancellationToken cancellationToken = default);

    #endregion

    #region Backup Operations

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
    /// Creates a new backup of a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to backup.</param>
    /// <param name="options">Options controlling the backup creation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the created backup identifier.</returns>
    Task<Result<string>> CreateBackupAsync(
        string instanceId,
        CreateBackupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores an instance from a specific backup.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to restore.</param>
    /// <param name="backupId">The unique identifier of the backup to restore from.</param>
    /// <param name="options">Options controlling the restore behavior.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> RestoreBackupAsync(
        string instanceId,
        string backupId,
        RestoreBackupOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a backup file as a stream.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="backupId">The unique identifier of the backup to download.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a stream of the backup file data.</returns>
    Task<Result<Stream>> DownloadBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific backup.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="backupId">The unique identifier of the backup to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> DeleteBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Update Management

    /// <summary>
    /// Checks if updates are available for a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing information about available updates.</returns>
    Task<Result<UpdateAvailability>> CheckForUpdatesAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies available updates to a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance to update.</param>
    /// <param name="options">Options controlling the update behavior.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing information about the update result.</returns>
    Task<Result<UpdateResult>> ApplyUpdatesAsync(
        string instanceId,
        ApplyUpdatesOptions? options = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Configuration Management

    /// <summary>
    /// Gets the current configuration of a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the instance configuration as key-value pairs.</returns>
    Task<Result<IReadOnlyDictionary<string, string>>> GetConfigurationAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a proposed configuration without applying it.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="configuration">The configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the validation result.</returns>
    Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a new configuration to a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="configuration">The configuration to apply.</param>
    /// <param name="options">Options controlling the configuration application.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing information about the configuration application result.</returns>
    Task<Result<ApplyConfigurationResult>> ApplyConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        ApplyConfigurationOptions? options = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Observability

    /// <summary>
    /// Retrieves log entries from a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="options">Options controlling which logs to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing a read-only list of log entries.</returns>
    Task<Result<IReadOnlyList<LogEntry>>> GetLogsAsync(
        string instanceId,
        GetLogsOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams log entries from a specific instance in real-time.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token to stop the stream.</param>
    /// <returns>An async enumerable that yields log entries as they are generated.</returns>
    IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on a specific instance.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the health check result.</returns>
    Task<Result<HealthCheckResult>> HealthCheckAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Utility Operations

    /// <summary>
    /// Sends a chat message to the in-game server console.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> SendChatMessageAsync(
        string instanceId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually triggers a world save operation.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure of the operation.</returns>
    Task<Result<Unit>> SaveWorldAsync(
        string instanceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom RCON command on the server.
    /// </summary>
    /// <param name="instanceId">The unique identifier of the instance.</param>
    /// <param name="command">The RCON command to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the command response.</returns>
    Task<Result<string>> ExecuteCustomCommandAsync(
        string instanceId,
        string command,
        CancellationToken cancellationToken = default);

    #endregion
}
