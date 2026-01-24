using System.Runtime.CompilerServices;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.Tests.Fakes;

/// <summary>
/// Fake implementation of IPokManagerClient for testing.
/// Provides full control over behavior and state for deterministic testing.
/// </summary>
public class FakePokManagerClient : IPokManagerClient
{
    // State tracking
    private readonly Dictionary<string, InstanceState> _instanceStates = new();
    private readonly Dictionary<string, List<BackupInfo>> _backups = new();
    private readonly Dictionary<string, InstanceDetails> _instanceDetails = new();
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _configurations = new();
    private readonly Dictionary<string, List<LogEntry>> _logs = new();
    private readonly Dictionary<string, UpdateAvailability> _updateInfo = new();
    private readonly Dictionary<string, ApplyConfigurationResult> _applyConfigurationResults = new();
    private readonly List<string> _methodCalls = new();

    // Behavior control
    private bool _shouldFailNextOperation = false;
    private string? _nextOperationError = null;
    private TimeSpan? _simulatedDelay = null;

    #region Setup Methods for Test Control

    /// <summary>
    /// Set up an instance with a specific state.
    /// </summary>
    public void SetupInstance(string instanceId, InstanceState state)
    {
        _instanceStates[instanceId] = state;
    }

    /// <summary>
    /// Set up detailed information for an instance.
    /// </summary>
    public void SetupInstanceDetails(string instanceId, InstanceDetails details)
    {
        _instanceDetails[instanceId] = details;
        _instanceStates[instanceId] = details.State;
    }

    /// <summary>
    /// Set up backups for an instance.
    /// </summary>
    public void SetupBackups(string instanceId, List<BackupInfo> backups)
    {
        _backups[instanceId] = backups;
    }

    /// <summary>
    /// Set up configuration for an instance.
    /// </summary>
    public void SetupConfiguration(string instanceId, IReadOnlyDictionary<string, string> configuration)
    {
        _configurations[instanceId] = configuration;
    }

    /// <summary>
    /// Set up logs for an instance.
    /// </summary>
    public void SetupLogs(string instanceId, List<LogEntry> logs)
    {
        _logs[instanceId] = logs;
    }

    /// <summary>
    /// Set up update availability information for an instance.
    /// </summary>
    public void SetupUpdateInfo(string instanceId, UpdateAvailability updateInfo)
    {
        _updateInfo[instanceId] = updateInfo;
    }

    /// <summary>
    /// Set up a custom ApplyConfigurationResult for an instance.
    /// </summary>
    public void SetupApplyConfigurationResult(string instanceId, ApplyConfigurationResult result)
    {
        _applyConfigurationResults[instanceId] = result;
    }

    /// <summary>
    /// Make the next operation fail with the specified error message.
    /// </summary>
    public void FailNextOperation(string errorMessage)
    {
        _shouldFailNextOperation = true;
        _nextOperationError = errorMessage;
    }

    /// <summary>
    /// Simulate a delay for all operations.
    /// </summary>
    public void SimulateDelay(TimeSpan delay)
    {
        _simulatedDelay = delay;
    }

    /// <summary>
    /// Clear simulated delay.
    /// </summary>
    public void ClearDelay()
    {
        _simulatedDelay = null;
    }

    /// <summary>
    /// Reset all state and behavior to defaults.
    /// </summary>
    public void Reset()
    {
        _instanceStates.Clear();
        _backups.Clear();
        _instanceDetails.Clear();
        _configurations.Clear();
        _logs.Clear();
        _updateInfo.Clear();
        _methodCalls.Clear();
        _shouldFailNextOperation = false;
        _nextOperationError = null;
        _simulatedDelay = null;
    }

    /// <summary>
    /// Get a read-only list of all method calls made.
    /// </summary>
    public IReadOnlyList<string> GetMethodCalls() => _methodCalls.AsReadOnly();

    /// <summary>
    /// Check if a specific method was called.
    /// </summary>
    public bool WasMethodCalled(string methodName) => _methodCalls.Contains(methodName);

    /// <summary>
    /// Get the number of times a method was called.
    /// </summary>
    public int GetMethodCallCount(string methodName) => _methodCalls.Count(m => m == methodName);

    #endregion

    #region Helper Methods

    private async Task ApplyDelayIfConfigured(CancellationToken cancellationToken)
    {
        if (_simulatedDelay.HasValue)
        {
            await Task.Delay(_simulatedDelay.Value, cancellationToken);
        }
    }

    private Result<T> CheckForFailure<T>()
    {
        if (_shouldFailNextOperation)
        {
            _shouldFailNextOperation = false;
            var error = _nextOperationError ?? "Operation failed";
            _nextOperationError = null;
            return Result.Failure<T>(error);
        }
        return null!; // No failure
    }

    #endregion

    #region Discovery & Query

    public async Task<Result<IReadOnlyList<string>>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ListInstancesAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<IReadOnlyList<string>>();
        if (failure != null) return failure;

        return Result<IReadOnlyList<string>>.Success(_instanceStates.Keys.ToList());
    }

    public async Task<Result<InstanceStatus>> GetInstanceStatusAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(GetInstanceStatusAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<InstanceStatus>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<InstanceStatus>("InstanceNotFound");

        var status = new InstanceStatus(
            instanceId,
            _instanceStates[instanceId],
            ProcessHealth.Healthy,
            TimeSpan.FromHours(1),
            0,
            32,
            "1.0.0",
            DateTimeOffset.UtcNow);

        return Result<InstanceStatus>.Success(status);
    }

    public async Task<Result<InstanceDetails>> GetInstanceDetailsAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(GetInstanceDetailsAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<InstanceDetails>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<InstanceDetails>("InstanceNotFound");

        if (_instanceDetails.TryGetValue(instanceId, out var details))
        {
            return Result<InstanceDetails>.Success(details);
        }

        // Create default details if not explicitly set
        var defaultDetails = new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            _instanceStates[instanceId],
            ProcessHealth.Healthy,
            8211,
            32,
            0,
            "1.0.0",
            TimeSpan.FromHours(1),
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            null,
            new Dictionary<string, string>());

        return Result<InstanceDetails>.Success(defaultDetails);
    }

    #endregion

    #region Lifecycle Management

    public async Task<Result<string>> CreateInstanceAsync(
        CreateInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(CreateInstanceAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<string>();
        if (failure != null) return failure;

        if (_instanceStates.ContainsKey(request.InstanceId))
            return Result.Failure<string>("InstanceAlreadyExists");

        _instanceStates[request.InstanceId] = request.AutoStart ? InstanceState.Running : InstanceState.Stopped;

        return Result<string>.Success(request.InstanceId);
    }

    public async Task<Result<Unit>> StartInstanceAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(StartInstanceAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        var currentState = _instanceStates[instanceId];
        if (currentState == InstanceState.Running)
            return Result.Failure<Unit>("InstanceAlreadyRunning");

        _instanceStates[instanceId] = InstanceState.Running;
        return Result.Success();
    }

    public async Task<Result<Unit>> StopInstanceAsync(
        string instanceId,
        StopInstanceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(StopInstanceAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        var currentState = _instanceStates[instanceId];
        if (currentState == InstanceState.Stopped)
            return Result.Failure<Unit>("InstanceAlreadyStopped");

        _instanceStates[instanceId] = InstanceState.Stopped;
        return Result.Success();
    }

    public async Task<Result<Unit>> RestartInstanceAsync(
        string instanceId,
        RestartInstanceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(RestartInstanceAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        _instanceStates[instanceId] = InstanceState.Running;
        return Result.Success();
    }

    public async Task<Result<Unit>> DeleteInstanceAsync(
        string instanceId,
        bool deleteBackups = false,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(DeleteInstanceAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        _instanceStates.Remove(instanceId);
        _instanceDetails.Remove(instanceId);
        _configurations.Remove(instanceId);
        _logs.Remove(instanceId);
        _updateInfo.Remove(instanceId);

        if (deleteBackups)
        {
            _backups.Remove(instanceId);
        }

        return Result.Success();
    }

    #endregion

    #region Backup Operations

    public async Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ListBackupsAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<IReadOnlyList<BackupInfo>>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<IReadOnlyList<BackupInfo>>("InstanceNotFound");

        var backups = _backups.TryGetValue(instanceId, out var list)
            ? list
            : new List<BackupInfo>();

        return Result<IReadOnlyList<BackupInfo>>.Success(backups);
    }

    public async Task<Result<string>> CreateBackupAsync(
        string instanceId,
        CreateBackupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(CreateBackupAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<string>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<string>("InstanceNotFound");

        var backupId = $"backup_{Guid.NewGuid():N}";
        var backup = new BackupInfo(
            backupId,
            instanceId,
            options?.Description,
            options?.CompressionFormat ?? CompressionFormat.Gzip,
            1024 * 1024 * 100, // 100 MB
            DateTimeOffset.UtcNow,
            $"/backups/{instanceId}/{backupId}.gz",
            false,
            "1.0.0");

        if (!_backups.ContainsKey(instanceId))
            _backups[instanceId] = new List<BackupInfo>();

        _backups[instanceId].Add(backup);

        return Result<string>.Success(backupId);
    }

    public async Task<Result<Unit>> RestoreBackupAsync(
        string instanceId,
        string backupId,
        RestoreBackupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(RestoreBackupAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        if (!_backups.TryGetValue(instanceId, out var backups) ||
            !backups.Any(b => b.BackupId == backupId))
            return Result.Failure<Unit>("BackupNotFound");

        return Result.Success();
    }

    public async Task<Result<Stream>> DownloadBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(DownloadBackupAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Stream>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Stream>("InstanceNotFound");

        if (!_backups.TryGetValue(instanceId, out var backups) ||
            !backups.Any(b => b.BackupId == backupId))
            return Result.Failure<Stream>("BackupNotFound");

        // Return empty memory stream for testing
        var stream = new MemoryStream();
        return Result<Stream>.Success(stream);
    }

    public async Task<Result<Unit>> DeleteBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(DeleteBackupAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        if (!_backups.TryGetValue(instanceId, out var backups))
            return Result.Failure<Unit>("BackupNotFound");

        var backup = backups.FirstOrDefault(b => b.BackupId == backupId);
        if (backup == null)
            return Result.Failure<Unit>("BackupNotFound");

        backups.Remove(backup);
        return Result.Success();
    }

    #endregion

    #region Update Management

    public async Task<Result<UpdateAvailability>> CheckForUpdatesAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(CheckForUpdatesAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<UpdateAvailability>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<UpdateAvailability>("InstanceNotFound");

        if (_updateInfo.TryGetValue(instanceId, out var update))
        {
            return Result<UpdateAvailability>.Success(update);
        }

        // Default: no updates available
        var defaultUpdate = new UpdateAvailability(
            false,
            "1.0.0",
            "1.0.0",
            null,
            null,
            false,
            DateTimeOffset.UtcNow);

        return Result<UpdateAvailability>.Success(defaultUpdate);
    }

    public async Task<Result<UpdateResult>> ApplyUpdatesAsync(
        string instanceId,
        ApplyUpdatesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ApplyUpdatesAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<UpdateResult>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<UpdateResult>("InstanceNotFound");

        var result = new UpdateResult(
            true,
            "1.0.0",
            "1.1.0",
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(5),
            "Update completed successfully",
            true,
            true);

        return Result<UpdateResult>.Success(result);
    }

    #endregion

    #region Configuration Management

    public async Task<Result<IReadOnlyDictionary<string, string>>> GetConfigurationAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(GetConfigurationAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<IReadOnlyDictionary<string, string>>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<IReadOnlyDictionary<string, string>>("InstanceNotFound");

        var config = _configurations.TryGetValue(instanceId, out var cfg)
            ? cfg
            : new Dictionary<string, string>();

        return Result<IReadOnlyDictionary<string, string>>.Success(config);
    }

    public async Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ValidateConfigurationAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<ConfigurationValidationResult>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<ConfigurationValidationResult>("InstanceNotFound");

        var result = new ConfigurationValidationResult(
            true,
            Array.Empty<string>(),
            Array.Empty<string>(),
            DateTimeOffset.UtcNow);

        return Result<ConfigurationValidationResult>.Success(result);
    }

    public async Task<Result<ApplyConfigurationResult>> ApplyConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        ApplyConfigurationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ApplyConfigurationAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<ApplyConfigurationResult>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<ApplyConfigurationResult>("InstanceNotFound");

        _configurations[instanceId] = configuration;

        // Use custom result if configured, otherwise use default
        ApplyConfigurationResult result;
        if (_applyConfigurationResults.TryGetValue(instanceId, out var customResult))
        {
            result = customResult;
        }
        else
        {
            result = new ApplyConfigurationResult(
                true,
                configuration.Keys.ToList(),
                false,
                false,
                true,
                DateTimeOffset.UtcNow,
                "Configuration applied successfully");
        }

        return Result<ApplyConfigurationResult>.Success(result);
    }

    #endregion

    #region Observability

    public async Task<Result<IReadOnlyList<LogEntry>>> GetLogsAsync(
        string instanceId,
        GetLogsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(GetLogsAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<IReadOnlyList<LogEntry>>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<IReadOnlyList<LogEntry>>("InstanceNotFound");

        var logs = _logs.TryGetValue(instanceId, out var list)
            ? list
            : new List<LogEntry>();

        return Result<IReadOnlyList<LogEntry>>.Success(logs);
    }

    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string instanceId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(StreamLogsAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        if (_logs.TryGetValue(instanceId, out var logs))
        {
            foreach (var log in logs)
            {
                if (cancellationToken.IsCancellationRequested)
                    yield break;

                yield return log;
                await Task.Delay(100, cancellationToken);
            }
        }
    }

    public async Task<Result<HealthCheckResult>> HealthCheckAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(HealthCheckAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<HealthCheckResult>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<HealthCheckResult>("InstanceNotFound");

        var result = new HealthCheckResult(
            instanceId,
            true,
            ProcessHealth.Healthy,
            TimeSpan.FromMilliseconds(50),
            DateTimeOffset.UtcNow,
            "Instance is healthy",
            new Dictionary<string, string> { ["status"] = "ok" });

        return Result<HealthCheckResult>.Success(result);
    }

    #endregion

    #region Utility Operations

    public async Task<Result<Unit>> SendChatMessageAsync(
        string instanceId,
        string message,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(SendChatMessageAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        if (_instanceStates[instanceId] != InstanceState.Running)
            return Result.Failure<Unit>("InstanceNotRunning");

        return Result.Success();
    }

    public async Task<Result<Unit>> SaveWorldAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(SaveWorldAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<Unit>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<Unit>("InstanceNotFound");

        if (_instanceStates[instanceId] != InstanceState.Running)
            return Result.Failure<Unit>("InstanceNotRunning");

        return Result.Success();
    }

    public async Task<Result<string>> ExecuteCustomCommandAsync(
        string instanceId,
        string command,
        CancellationToken cancellationToken = default)
    {
        _methodCalls.Add(nameof(ExecuteCustomCommandAsync));
        await ApplyDelayIfConfigured(cancellationToken);

        var failure = CheckForFailure<string>();
        if (failure != null) return failure;

        if (!_instanceStates.ContainsKey(instanceId))
            return Result.Failure<string>("InstanceNotFound");

        if (_instanceStates[instanceId] != InstanceState.Running)
            return Result.Failure<string>("InstanceNotRunning");

        return Result<string>.Success($"Command '{command}' executed successfully");
    }

    #endregion
}
