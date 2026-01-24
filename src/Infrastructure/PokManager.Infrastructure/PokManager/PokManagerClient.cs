using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using PokManager.Infrastructure.Shell;

namespace PokManager.Infrastructure.PokManager;

/// <summary>
/// Implementation of IPokManagerClient that interacts with the POK Manager bash script.
/// Executes bash commands and parses their output to manage Palworld server instances.
/// </summary>
public sealed class PokManagerClient : IPokManagerClient
{
    private readonly IBashCommandExecutor _bashExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly ILogger<PokManagerClient> _logger;

    public PokManagerClient(
        IBashCommandExecutor bashExecutor,
        PokManagerClientConfiguration configuration,
        ILogger<PokManagerClient> logger)
    {
        _bashExecutor = bashExecutor ?? throw new ArgumentNullException(nameof(bashExecutor));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _configuration.Validate();
    }

    #region Discovery & Query - IMPLEMENTED

    public async Task<Result<IReadOnlyList<string>>> ListInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Listing instances from {BasePath}", _configuration.InstancesBasePath);

            // List directories in the instances base path
            var command = OperatingSystem.IsWindows()
                ? $"dir /b \"{_configuration.InstancesBasePath}\""
                : $"ls -1 \"{_configuration.InstancesBasePath}\"";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<string>>(
                    $"Failed to list instances: {result.StdErr}");
            }

            // Parse the output to find Instance_* directories
            var lines = result.StdOut
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("Instance_", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Extract instance IDs by removing the "Instance_" prefix
            var instanceIds = lines
                .Select(line => line.Substring("Instance_".Length))
                .ToList();

            _logger.LogDebug("Found {Count} instances", instanceIds.Count);

            return Result<IReadOnlyList<string>>.Success(instanceIds);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while listing instances");
            return Result.Failure<IReadOnlyList<string>>(
                $"Operation timed out while listing instances: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "List instances operation was cancelled");
            return Result.Failure<IReadOnlyList<string>>(
                "Operation was cancelled while listing instances");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing instances");
            return Result.Failure<IReadOnlyList<string>>(
                $"Unexpected error while listing instances: {ex.Message}");
        }
    }

    public async Task<Result<InstanceStatus>> GetInstanceStatusAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Getting status for instance {InstanceId}", instanceId);

            // Execute the POK Manager status command
            var command = $"\"{_configuration.PokManagerScriptPath}\" status {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<InstanceStatus>(
                    $"Failed to get status for instance '{instanceId}': {result.StdErr}");
            }

            // Parse the status output
            var status = ParseStatusOutput(instanceId, result.StdOut);

            _logger.LogDebug(
                "Instance {InstanceId} status: {State}",
                instanceId,
                status.State);

            return Result<InstanceStatus>.Success(status);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while getting status for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceStatus>(
                $"Operation timed out while getting status: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get status operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceStatus>(
                "Operation was cancelled while getting status");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceStatus>(
                $"Unexpected error while getting status: {ex.Message}");
        }
    }

    public async Task<Result<InstanceDetails>> GetInstanceDetailsAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Getting details for instance {InstanceId}", instanceId);

            // Execute the POK Manager details/info command
            var command = $"\"{_configuration.PokManagerScriptPath}\" details {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<InstanceDetails>(
                    $"Failed to get details for instance '{instanceId}': {result.StdErr}");
            }

            // Parse the details output
            var details = ParseDetailsOutput(instanceId, result.StdOut);

            _logger.LogDebug(
                "Instance {InstanceId} details retrieved: {ServerName}",
                instanceId,
                details.ServerName);

            return Result<InstanceDetails>.Success(details);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while getting details for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceDetails>(
                $"Operation timed out while getting details: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get details operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceDetails>(
                "Operation was cancelled while getting details");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting details for instance {InstanceId}", instanceId);
            return Result.Failure<InstanceDetails>(
                $"Unexpected error while getting details: {ex.Message}");
        }
    }

    #endregion

    #region Lifecycle Management - IMPLEMENTED

    public async Task<Result<string>> CreateInstanceAsync(
        CreateInstanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            _logger.LogDebug("Creating instance {InstanceId}", request.InstanceId);

            // Create InstanceId value object
            var instanceIdResult = InstanceId.Create(request.InstanceId);
            if (instanceIdResult.IsFailure)
            {
                return Result.Failure<string>($"Invalid instance ID: {instanceIdResult.Error}");
            }

            // Build the create command
            var commandBuilder = CreateInstanceCommandBuilder
                .Create(_configuration.PokManagerScriptPath)
                .ForInstance(instanceIdResult.Value)
                .WithServerName(request.ServerName)
                .WithPort(request.Port)
                .WithMaxPlayers(request.MaxPlayers);

            // Add optional password
            if (!string.IsNullOrWhiteSpace(request.ServerPassword))
            {
                var passwordResult = ServerPassword.Create(request.ServerPassword);
                if (passwordResult.IsSuccess)
                {
                    commandBuilder.WithPassword(passwordResult.Value);
                }
            }

            // Add auto-start flag if requested
            if (request.AutoStart)
            {
                commandBuilder.WithStartAfterCreate();
            }

            var commandResult = commandBuilder.Build();
            if (commandResult.IsFailure)
            {
                return Result.Failure<string>($"Failed to build create command: {commandResult.Error}");
            }

            _logger.LogDebug("Executing create command: {Command}", commandResult.Value);

            // Execute the command
            var result = await _bashExecutor.ExecuteAsync(
                commandResult.Value,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<string>(
                    $"Failed to create instance '{request.InstanceId}': {result.StdErr}");
            }

            _logger.LogInformation("Instance {InstanceId} created successfully", request.InstanceId);

            return Result<string>.Success(request.InstanceId);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while creating instance {InstanceId}", request.InstanceId);
            return Result.Failure<string>(
                $"Operation timed out while creating instance: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create instance operation was cancelled for {InstanceId}", request.InstanceId);
            return Result.Failure<string>(
                "Operation was cancelled while creating instance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instance {InstanceId}", request.InstanceId);
            return Result.Failure<string>(
                $"Unexpected error while creating instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> StartInstanceAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Starting instance {InstanceId}", instanceId);

            // Create InstanceId value object
            var instanceIdResult = InstanceId.Create(instanceId);
            if (instanceIdResult.IsFailure)
            {
                return Result.Failure<Unit>($"Invalid instance ID: {instanceIdResult.Error}");
            }

            // Build the start command
            var commandResult = StartCommandBuilder
                .Create(_configuration.PokManagerScriptPath)
                .ForInstance(instanceIdResult.Value)
                .Build();

            if (commandResult.IsFailure)
            {
                return Result.Failure<Unit>($"Failed to build start command: {commandResult.Error}");
            }

            _logger.LogDebug("Executing start command: {Command}", commandResult.Value);

            // Execute the command
            var result = await _bashExecutor.ExecuteAsync(
                commandResult.Value,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to start instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation("Instance {InstanceId} started successfully", instanceId);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while starting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while starting instance: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Start instance operation was cancelled for {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while starting instance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while starting instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> StopInstanceAsync(
        string instanceId,
        StopInstanceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Stopping instance {InstanceId}", instanceId);

            // Create InstanceId value object
            var instanceIdResult = InstanceId.Create(instanceId);
            if (instanceIdResult.IsFailure)
            {
                return Result.Failure<Unit>($"Invalid instance ID: {instanceIdResult.Error}");
            }

            // Build the stop command
            var commandBuilder = StopCommandBuilder
                .Create(_configuration.PokManagerScriptPath)
                .ForInstance(instanceIdResult.Value);

            // Apply options
            if (options != null)
            {
                if (options.SaveWorld)
                {
                    commandBuilder.WithSave();
                }

                if (!options.Graceful)
                {
                    commandBuilder.WithForce();
                }

                if (options.Timeout.HasValue)
                {
                    commandBuilder.WithGracePeriod((int)options.Timeout.Value.TotalSeconds);
                }
            }

            var commandResult = commandBuilder.Build();
            if (commandResult.IsFailure)
            {
                return Result.Failure<Unit>($"Failed to build stop command: {commandResult.Error}");
            }

            _logger.LogDebug("Executing stop command: {Command}", commandResult.Value);

            // Execute the command
            var result = await _bashExecutor.ExecuteAsync(
                commandResult.Value,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to stop instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation("Instance {InstanceId} stopped successfully", instanceId);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while stopping instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while stopping instance: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Stop instance operation was cancelled for {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while stopping instance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while stopping instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> RestartInstanceAsync(
        string instanceId,
        RestartInstanceOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Restarting instance {InstanceId}", instanceId);

            // Create InstanceId value object
            var instanceIdResult = InstanceId.Create(instanceId);
            if (instanceIdResult.IsFailure)
            {
                return Result.Failure<Unit>($"Invalid instance ID: {instanceIdResult.Error}");
            }

            // Build the restart command
            var commandBuilder = RestartCommandBuilder
                .Create(_configuration.PokManagerScriptPath)
                .ForInstance(instanceIdResult.Value);

            // Apply options
            if (options != null)
            {
                if (options.SaveWorld)
                {
                    commandBuilder.WithSave();
                }

                if (options.WaitForHealthy)
                {
                    commandBuilder.WithWait();
                }

                if (options.Timeout.HasValue)
                {
                    commandBuilder.WithGracePeriod((int)options.Timeout.Value.TotalSeconds);
                }
            }

            var commandResult = commandBuilder.Build();
            if (commandResult.IsFailure)
            {
                return Result.Failure<Unit>($"Failed to build restart command: {commandResult.Error}");
            }

            _logger.LogDebug("Executing restart command: {Command}", commandResult.Value);

            // Execute the command
            var result = await _bashExecutor.ExecuteAsync(
                commandResult.Value,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to restart instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation("Instance {InstanceId} restarted successfully", instanceId);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while restarting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while restarting instance: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Restart instance operation was cancelled for {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while restarting instance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while restarting instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> DeleteInstanceAsync(
        string instanceId,
        bool deleteBackups = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Deleting instance {InstanceId}", instanceId);

            // Create InstanceId value object
            var instanceIdResult = InstanceId.Create(instanceId);
            if (instanceIdResult.IsFailure)
            {
                return Result.Failure<Unit>($"Invalid instance ID: {instanceIdResult.Error}");
            }

            // Build the delete command
            var commandBuilder = PokManagerCommandBuilder
                .Create(_configuration.PokManagerScriptPath)
                .WithCommand("delete")
                .WithInstanceId(instanceIdResult.Value);

            // Add delete-backups flag if requested
            if (deleteBackups)
            {
                commandBuilder.WithFlag("delete-backups");
            }

            // Always add force flag for delete operations
            commandBuilder.WithFlag("force");

            var commandResult = commandBuilder.Build();
            if (commandResult.IsFailure)
            {
                return Result.Failure<Unit>($"Failed to build delete command: {commandResult.Error}");
            }

            _logger.LogDebug("Executing delete command: {Command}", commandResult.Value);

            // Execute the command
            var result = await _bashExecutor.ExecuteAsync(
                commandResult.Value,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to delete instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation("Instance {InstanceId} deleted successfully", instanceId);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while deleting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while deleting instance: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete instance operation was cancelled for {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while deleting instance");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while deleting instance: {ex.Message}");
        }
    }

    #endregion

    #region Backup Operations - IMPLEMENTED

    public async Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Listing backups for instance {InstanceId}", instanceId);

            // Build the backup list command
            var backupPath = Path.Combine(_configuration.InstancesBasePath, "backups");
            var command = OperatingSystem.IsWindows()
                ? $"dir /b \"{backupPath}\\backup_{instanceId}_*.tar.*\""
                : $"ls -1 \"{backupPath}/backup_{instanceId}_*.tar.*\"";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<BackupInfo>>(
                    $"Failed to list backups for instance '{instanceId}': {result.StdErr}");
            }

            // Parse the backup list output
            var parseResult = ParseBackupListOutput(instanceId, result.StdOut, backupPath);
            if (parseResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<BackupInfo>>(parseResult.Error);
            }

            _logger.LogDebug(
                "Found {Count} backups for instance {InstanceId}",
                parseResult.Value.Count,
                instanceId);

            return Result<IReadOnlyList<BackupInfo>>.Success(parseResult.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while listing backups for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<BackupInfo>>(
                $"Operation timed out while listing backups: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "List backups operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<BackupInfo>>(
                "Operation was cancelled while listing backups");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing backups for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<BackupInfo>>(
                $"Unexpected error while listing backups: {ex.Message}");
        }
    }

    public async Task<Result<string>> CreateBackupAsync(
        string instanceId,
        CreateBackupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Creating backup for instance {InstanceId}", instanceId);

            // Use default options if not provided
            options ??= new CreateBackupOptions();

            // Build the backup command
            var command = $"\"{_configuration.PokManagerScriptPath}\" backup {instanceId}";

            // Add compression format
            var compressionArg = options.CompressionFormat switch
            {
                CompressionFormat.Gzip => " --compress gzip",
                CompressionFormat.Zstd => " --compress zstd",
                _ => " --compress gzip" // Default to gzip
            };
            command += compressionArg;

            // Add exclude logs flag if needed
            if (!options.IncludeLogs)
            {
                command += " --exclude-logs";
            }

            // Add description if provided
            if (!string.IsNullOrWhiteSpace(options.Description))
            {
                command += $" --description \"{options.Description}\"";
            }

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<string>(
                    $"Failed to create backup for instance '{instanceId}': {result.StdErr}");
            }

            // Parse the backup creation output to extract backup ID
            var backupId = ParseBackupCreationOutput(result.StdOut);
            if (string.IsNullOrEmpty(backupId))
            {
                return Result.Failure<string>(
                    "Failed to parse backup ID from command output");
            }

            _logger.LogInformation(
                "Successfully created backup {BackupId} for instance {InstanceId}",
                backupId,
                instanceId);

            return Result<string>.Success(backupId);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while creating backup for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                $"Operation timed out while creating backup: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Create backup operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                "Operation was cancelled while creating backup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating backup for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                $"Unexpected error while creating backup: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> RestoreBackupAsync(
        string instanceId,
        string backupId,
        RestoreBackupOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(backupId))
        {
            throw new ArgumentException("Backup ID cannot be null or empty", nameof(backupId));
        }

        try
        {
            _logger.LogDebug("Restoring backup {BackupId} for instance {InstanceId}", backupId, instanceId);

            // Use default options if not provided
            options ??= new RestoreBackupOptions();

            // Build the restore command
            var command = $"\"{_configuration.PokManagerScriptPath}\" restore {instanceId} --backup-id {backupId}";

            // Add stop-before-restore flag if needed
            if (options.StopInstance)
            {
                command += " --stop-before-restore";
            }

            // Add start-after-restore flag if needed
            if (options.StartAfterRestore)
            {
                command += " --start-after-restore";
            }

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to restore backup '{backupId}' for instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation(
                "Successfully restored backup {BackupId} for instance {InstanceId}",
                backupId,
                instanceId);

            return Result.Success();
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while restoring backup {BackupId} for instance {InstanceId}", backupId, instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while restoring backup: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Restore backup operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while restoring backup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring backup {BackupId} for instance {InstanceId}", backupId, instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while restoring backup: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> DownloadBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(backupId))
        {
            throw new ArgumentException("Backup ID cannot be null or empty", nameof(backupId));
        }

        try
        {
            _logger.LogDebug("Downloading backup {BackupId} for instance {InstanceId}", backupId, instanceId);

            // Build the backup file path
            var backupPath = Path.Combine(_configuration.InstancesBasePath, "backups", backupId);

            // Check if file exists using a command
            var checkCommand = OperatingSystem.IsWindows()
                ? $"if exist \"{backupPath}\" echo EXISTS"
                : $"test -f \"{backupPath}\" && echo EXISTS";

            var checkResult = await _bashExecutor.ExecuteAsync(
                checkCommand,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!checkResult.IsSuccess || !checkResult.StdOut.Contains("EXISTS"))
            {
                return Result.Failure<Stream>(
                    $"Failed to download backup '{backupId}': Backup file not found");
            }

            // Open the file stream for reading
            var fileStream = new FileStream(
                backupPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            _logger.LogInformation(
                "Successfully opened backup file {BackupId} for download",
                backupId);

            return Result<Stream>.Success(fileStream);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Backup file {BackupId} not found", backupId);
            return Result.Failure<Stream>(
                $"Backup file not found: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied to backup file {BackupId}", backupId);
            return Result.Failure<Stream>(
                $"Access denied to backup file: {ex.Message}");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while downloading backup {BackupId}", backupId);
            return Result.Failure<Stream>(
                $"Operation timed out while downloading backup: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Download backup operation was cancelled for {BackupId}", backupId);
            return Result.Failure<Stream>(
                "Operation was cancelled while downloading backup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading backup {BackupId}", backupId);
            return Result.Failure<Stream>(
                $"Unexpected error while downloading backup: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> DeleteBackupAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(backupId))
        {
            throw new ArgumentException("Backup ID cannot be null or empty", nameof(backupId));
        }

        try
        {
            _logger.LogDebug("Deleting backup {BackupId} for instance {InstanceId}", backupId, instanceId);

            // Build the delete backup command
            var backupPath = Path.Combine(_configuration.InstancesBasePath, "backups", backupId);
            var command = OperatingSystem.IsWindows()
                ? $"del /f \"{backupPath}\""
                : $"rm -f \"{backupPath}\"";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to delete backup '{backupId}': {result.StdErr}");
            }

            _logger.LogInformation(
                "Successfully deleted backup {BackupId} for instance {InstanceId}",
                backupId,
                instanceId);

            return Result.Success();
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while deleting backup {BackupId}", backupId);
            return Result.Failure<Unit>(
                $"Operation timed out while deleting backup: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Delete backup operation was cancelled for {BackupId}", backupId);
            return Result.Failure<Unit>(
                "Operation was cancelled while deleting backup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting backup {BackupId}", backupId);
            return Result.Failure<Unit>(
                $"Unexpected error while deleting backup: {ex.Message}");
        }
    }

    #endregion

    #region Update Management - IMPLEMENTED

    public async Task<Result<UpdateAvailability>> CheckForUpdatesAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Checking for updates for instance {InstanceId}", instanceId);

            var command = $"\"{_configuration.PokManagerScriptPath}\" update check {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<UpdateAvailability>(
                    $"Failed to check for updates for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.UpdateOutputParser();
            var parseResult = parser.ParseCheckForUpdates(result.StdOut);

            if (parseResult.IsFailure)
            {
                return Result.Failure<UpdateAvailability>(
                    $"Failed to parse update check output: {parseResult.Error}");
            }

            _logger.LogDebug(
                "Update check complete for {InstanceId}: {UpdateAvailable}",
                instanceId,
                parseResult.Value.IsUpdateAvailable);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while checking for updates for instance {InstanceId}", instanceId);
            return Result.Failure<UpdateAvailability>(
                $"Operation timed out while checking for updates: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Check for updates operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<UpdateAvailability>(
                "Operation was cancelled while checking for updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for updates for instance {InstanceId}", instanceId);
            return Result.Failure<UpdateAvailability>(
                $"Unexpected error while checking for updates: {ex.Message}");
        }
    }

    public async Task<Result<UpdateResult>> ApplyUpdatesAsync(
        string instanceId,
        ApplyUpdatesOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Applying updates to instance {InstanceId}", instanceId);

            var commandBuilder = new System.Text.StringBuilder();
            commandBuilder.Append($"\"{_configuration.PokManagerScriptPath}\" update apply {instanceId}");

            if (options != null)
            {
                if (options.BackupBeforeUpdate)
                {
                    commandBuilder.Append(" --backup");
                }
                if (options.StopInstance)
                {
                    commandBuilder.Append(" --stop");
                }
                if (options.StartAfterUpdate)
                {
                    commandBuilder.Append(" --start");
                }
                if (options.ValidateAfterUpdate)
                {
                    commandBuilder.Append(" --validate");
                }
            }

            var command = commandBuilder.ToString();

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                TimeSpan.FromMinutes(10),
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<UpdateResult>(
                    $"Failed to apply updates for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.UpdateOutputParser();
            var parseResult = parser.ParseApplyUpdates(result.StdOut);

            if (parseResult.IsFailure)
            {
                return Result.Failure<UpdateResult>(
                    $"Failed to parse update output: {parseResult.Error}");
            }

            _logger.LogInformation(
                "Updates applied to {InstanceId}: {PreviousVersion} -> {NewVersion}",
                instanceId,
                parseResult.Value.PreviousVersion,
                parseResult.Value.NewVersion);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while applying updates to instance {InstanceId}", instanceId);
            return Result.Failure<UpdateResult>(
                $"Operation timed out while applying updates: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Apply updates operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<UpdateResult>(
                "Operation was cancelled while applying updates");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying updates to instance {InstanceId}", instanceId);
            return Result.Failure<UpdateResult>(
                $"Unexpected error while applying updates: {ex.Message}");
        }
    }

    #endregion

    #region Configuration Management - IMPLEMENTED

    public async Task<Result<IReadOnlyDictionary<string, string>>> GetConfigurationAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Getting configuration for instance {InstanceId}", instanceId);

            var command = $"\"{_configuration.PokManagerScriptPath}\" config get {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<IReadOnlyDictionary<string, string>>(
                    $"Failed to get configuration for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.ConfigOutputParser();
            var parseResult = parser.ParseConfiguration(result.StdOut);

            if (parseResult.IsFailure)
            {
                return Result.Failure<IReadOnlyDictionary<string, string>>(
                    $"Failed to parse configuration output: {parseResult.Error}");
            }

            _logger.LogDebug(
                "Configuration retrieved for {InstanceId}: {Count} settings",
                instanceId,
                parseResult.Value.Count);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while getting configuration for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyDictionary<string, string>>(
                $"Operation timed out while getting configuration: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get configuration operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyDictionary<string, string>>(
                "Operation was cancelled while getting configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyDictionary<string, string>>(
                $"Unexpected error while getting configuration: {ex.Message}");
        }
    }

    public async Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (configuration == null)
        {
            throw new ArgumentException("Configuration cannot be null", nameof(configuration));
        }

        try
        {
            _logger.LogDebug("Validating configuration for instance {InstanceId}", instanceId);

            var commandBuilder = new System.Text.StringBuilder();
            commandBuilder.Append($"\"{_configuration.PokManagerScriptPath}\" config validate {instanceId}");

            foreach (var (key, value) in configuration)
            {
                commandBuilder.Append($" --{key} \"{value}\"");
            }

            var command = commandBuilder.ToString();

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<ConfigurationValidationResult>(
                    $"Failed to validate configuration for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.ConfigOutputParser();
            var parseResult = parser.ParseValidation(result.StdOut);

            if (parseResult.IsFailure)
            {
                return Result.Failure<ConfigurationValidationResult>(
                    $"Failed to parse validation output: {parseResult.Error}");
            }

            _logger.LogDebug(
                "Configuration validation for {InstanceId}: {IsValid}",
                instanceId,
                parseResult.Value.IsValid);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while validating configuration for instance {InstanceId}", instanceId);
            return Result.Failure<ConfigurationValidationResult>(
                $"Operation timed out while validating configuration: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Validate configuration operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<ConfigurationValidationResult>(
                "Operation was cancelled while validating configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating configuration for instance {InstanceId}", instanceId);
            return Result.Failure<ConfigurationValidationResult>(
                $"Unexpected error while validating configuration: {ex.Message}");
        }
    }

    public async Task<Result<ApplyConfigurationResult>> ApplyConfigurationAsync(
        string instanceId,
        IReadOnlyDictionary<string, string> configuration,
        ApplyConfigurationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (configuration == null)
        {
            throw new ArgumentException("Configuration cannot be null", nameof(configuration));
        }

        try
        {
            _logger.LogDebug("Applying configuration to instance {InstanceId}", instanceId);

            var commandBuilder = new System.Text.StringBuilder();
            commandBuilder.Append($"\"{_configuration.PokManagerScriptPath}\" config apply {instanceId}");

            foreach (var (key, value) in configuration)
            {
                commandBuilder.Append($" --{key} \"{value}\"");
            }

            if (options != null)
            {
                if (options.ValidateBeforeApply)
                {
                    commandBuilder.Append(" --validate");
                }
                if (options.BackupBeforeApply)
                {
                    commandBuilder.Append(" --backup");
                }
                if (options.RestartIfNeeded)
                {
                    commandBuilder.Append(" --restart-if-needed");
                }
            }

            var command = commandBuilder.ToString();

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<ApplyConfigurationResult>(
                    $"Failed to apply configuration for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.ConfigOutputParser();
            var parseResult = parser.ParseApplyConfiguration(result.StdOut);

            if (parseResult.IsFailure)
            {
                return Result.Failure<ApplyConfigurationResult>(
                    $"Failed to parse apply configuration output: {parseResult.Error}");
            }

            _logger.LogInformation(
                "Configuration applied to {InstanceId}: {ChangedSettings} settings changed",
                instanceId,
                parseResult.Value.ChangedSettings.Count);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while applying configuration to instance {InstanceId}", instanceId);
            return Result.Failure<ApplyConfigurationResult>(
                $"Operation timed out while applying configuration: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Apply configuration operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<ApplyConfigurationResult>(
                "Operation was cancelled while applying configuration");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying configuration to instance {InstanceId}", instanceId);
            return Result.Failure<ApplyConfigurationResult>(
                $"Unexpected error while applying configuration: {ex.Message}");
        }
    }

    #endregion

    #region Observability - PARTIALLY IMPLEMENTED

    public async Task<Result<IReadOnlyList<LogEntry>>> GetLogsAsync(
        string instanceId,
        GetLogsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Getting logs for instance {InstanceId}", instanceId);

            var commandBuilder = new System.Text.StringBuilder();
            commandBuilder.Append($"\"{_configuration.PokManagerScriptPath}\" logs {instanceId}");

            if (options != null)
            {
                if (options.MaxLines > 0)
                {
                    commandBuilder.Append($" --tail {options.MaxLines}");
                }
                if (options.Since.HasValue)
                {
                    commandBuilder.Append($" --since \"{options.Since.Value:o}\"");
                }
                if (options.Until.HasValue)
                {
                    commandBuilder.Append($" --until \"{options.Until.Value:o}\"");
                }
                if (!string.IsNullOrWhiteSpace(options.Filter))
                {
                    commandBuilder.Append($" --filter \"{options.Filter}\"");
                }
                if (options.IncludeSystemLogs)
                {
                    commandBuilder.Append(" --system");
                }
            }

            var command = commandBuilder.ToString();

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<LogEntry>>(
                    $"Failed to get logs for instance '{instanceId}': {result.StdErr}");
            }

            var parser = new Parsers.LogOutputParser();
            var parseResult = parser.ParseLogs(result.StdOut, instanceId);

            if (parseResult.IsFailure)
            {
                return Result.Failure<IReadOnlyList<LogEntry>>(
                    $"Failed to parse logs output: {parseResult.Error}");
            }

            _logger.LogDebug(
                "Retrieved {Count} log entries for instance {InstanceId}",
                parseResult.Value.Count,
                instanceId);

            return parseResult;
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while getting logs for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<LogEntry>>(
                $"Operation timed out while getting logs: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Get logs operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<LogEntry>>(
                "Operation was cancelled while getting logs");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting logs for instance {InstanceId}", instanceId);
            return Result.Failure<IReadOnlyList<LogEntry>>(
                $"Unexpected error while getting logs: {ex.Message}");
        }
    }

    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string instanceId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        _logger.LogDebug("Starting log stream for instance {InstanceId}", instanceId);

        // This is a simplified implementation that yields log entries
        // In a real implementation, this would use a streaming process or tail -f equivalent
        // For now, throw NotImplementedException as streaming requires more complex infrastructure
        await Task.CompletedTask; // Satisfy async requirement

        throw new NotImplementedException(
            "Log streaming requires a streaming-capable bash executor. " +
            "Use GetLogsAsync with polling for continuous log monitoring.");

        // Unreachable code - here's how it would work with streaming support:
        // var command = $"\"{_configuration.PokManagerScriptPath}\" logs {instanceId} --follow";
        // await foreach (var line in _bashExecutor.ExecuteStreamingAsync(command, cancellationToken))
        // {
        //     var parser = new Parsers.LogOutputParser();
        //     var parseResult = parser.ParseLogLine(line, instanceId);
        //     if (parseResult.IsSuccess)
        //     {
        //         yield return parseResult.Value;
        //     }
        // }

        // Satisfy compiler - this line is unreachable but required for async iterator
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    public async Task<Result<HealthCheckResult>> HealthCheckAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        var startTime = DateTimeOffset.UtcNow;

        try
        {
            _logger.LogDebug("Performing health check for instance {InstanceId}", instanceId);

            // Build the health check command (using status command)
            var command = $"\"{_configuration.PokManagerScriptPath}\" status {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            var responseTime = DateTimeOffset.UtcNow - startTime;

            if (!result.IsSuccess)
            {
                return Result.Failure<HealthCheckResult>(
                    $"Failed to check health for instance '{instanceId}': {result.StdErr}");
            }

            // Parse the health status from output
            var health = ParseHealthStatus(result.StdOut);
            var isHealthy = health == ProcessHealth.Healthy;

            var healthCheckResult = new HealthCheckResult(
                InstanceId: instanceId,
                IsHealthy: isHealthy,
                Health: health,
                ResponseTime: responseTime,
                CheckedAt: DateTimeOffset.UtcNow,
                Message: isHealthy ? "Instance is healthy" : "Instance is not healthy",
                Details: null
            );

            _logger.LogDebug(
                "Health check for instance {InstanceId} completed: {Health}",
                instanceId,
                health);

            return Result<HealthCheckResult>.Success(healthCheckResult);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while checking health for instance {InstanceId}", instanceId);
            return Result.Failure<HealthCheckResult>(
                $"Operation timed out while checking health: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Health check operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<HealthCheckResult>(
                "Operation was cancelled while checking health");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for instance {InstanceId}", instanceId);
            return Result.Failure<HealthCheckResult>(
                $"Unexpected error while checking health: {ex.Message}");
        }
    }

    #endregion

    #region Utility Operations - IMPLEMENTED

    public async Task<Result<Unit>> SendChatMessageAsync(
        string instanceId,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }

        try
        {
            _logger.LogDebug("Sending chat message to instance {InstanceId}", instanceId);

            var command = $"\"{_configuration.PokManagerScriptPath}\" rcon {instanceId} --broadcast \"{message}\"";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to send chat message to instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation(
                "Chat message sent to instance {InstanceId}: {Message}",
                instanceId,
                message);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while sending chat message to instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while sending chat message: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Send chat message operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while sending chat message");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending chat message to instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while sending chat message: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> SaveWorldAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        try
        {
            _logger.LogDebug("Saving world for instance {InstanceId}", instanceId);

            var command = $"\"{_configuration.PokManagerScriptPath}\" rcon {instanceId} --save";

            var result = await _bashExecutor.ExecuteAsync(
                command,
                _configuration.WorkingDirectory,
                TimeSpan.FromMinutes(2), // Save can take longer
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<Unit>(
                    $"Failed to save world for instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogInformation("World saved successfully for instance {InstanceId}", instanceId);

            return Result<Unit>.Success(Unit.Value);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while saving world for instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Operation timed out while saving world: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Save world operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                "Operation was cancelled while saving world");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving world for instance {InstanceId}", instanceId);
            return Result.Failure<Unit>(
                $"Unexpected error while saving world: {ex.Message}");
        }
    }

    public async Task<Result<string>> ExecuteCustomCommandAsync(
        string instanceId,
        string command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            throw new ArgumentException("Instance ID cannot be null or empty", nameof(instanceId));
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        try
        {
            _logger.LogDebug("Executing custom command for instance {InstanceId}: {Command}", instanceId, command);

            var bashCommand = $"\"{_configuration.PokManagerScriptPath}\" {command} {instanceId}";

            var result = await _bashExecutor.ExecuteAsync(
                bashCommand,
                _configuration.WorkingDirectory,
                _configuration.DefaultTimeout,
                cancellationToken);

            if (!result.IsSuccess)
            {
                return Result.Failure<string>(
                    $"Failed to execute custom command for instance '{instanceId}': {result.StdErr}");
            }

            _logger.LogDebug("Custom command executed successfully for instance {InstanceId}", instanceId);

            return Result<string>.Success(result.StdOut);
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Timeout while executing custom command for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                $"Operation timed out while executing custom command: {ex.Message}");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Execute custom command operation was cancelled for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                "Operation was cancelled while executing custom command");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing custom command for instance {InstanceId}", instanceId);
            return Result.Failure<string>(
                $"Unexpected error while executing custom command: {ex.Message}");
        }
    }

    #endregion

    #region Private Helper Methods

    private ProcessHealth ParseHealthStatus(string output)
    {
        // Parse health status from command output
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.Contains("healthy", StringComparison.OrdinalIgnoreCase) &&
                trimmedLine.Contains("Health:", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessHealth.Healthy;
            }
            else if (trimmedLine.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessHealth.Unhealthy;
            }
            else if (trimmedLine.Contains("degraded", StringComparison.OrdinalIgnoreCase))
            {
                return ProcessHealth.Degraded;
            }
        }

        // If running, assume healthy; if stopped, assume not applicable
        if (output.Contains("running", StringComparison.OrdinalIgnoreCase))
        {
            return ProcessHealth.Healthy;
        }

        return ProcessHealth.Unknown;
    }

    private Result<IReadOnlyList<BackupInfo>> ParseBackupListOutput(string instanceId, string output, string backupBasePath)
    {
        // Handle null or empty gracefully - return empty list
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<IReadOnlyList<BackupInfo>>.Success(Array.Empty<BackupInfo>());
        }

        var backups = new List<BackupInfo>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Extract filename from path if present
            var fileName = Path.GetFileName(trimmedLine);

            // Parse backup filename: backup_<instanceId>_<YYYYMMDD>_<HHMMSS>.tar.[gz|zst]
            var backupPattern = new Regex(
                @"backup_(?<instanceId>.+?)_(?<date>\d{8})_(?<time>\d{6})\.tar\.(?<compression>gz|zst)",
                RegexOptions.IgnoreCase
            );

            var match = backupPattern.Match(fileName);
            if (!match.Success)
            {
                continue;
            }

            var backupInstanceId = match.Groups["instanceId"].Value;
            var dateStr = match.Groups["date"].Value;
            var timeStr = match.Groups["time"].Value;
            var compressionStr = match.Groups["compression"].Value.ToLowerInvariant();

            // Parse timestamp
            var dateTimeStr = $"{dateStr}{timeStr}";
            if (!DateTimeOffset.TryParseExact(
                dateTimeStr,
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal,
                out var timestamp))
            {
                continue;
            }

            // Determine compression format
            var compressionFormat = compressionStr switch
            {
                "gz" => CompressionFormat.Gzip,
                "zst" => CompressionFormat.Zstd,
                _ => CompressionFormat.Unknown
            };

            // Build full file path
            var filePath = Path.Combine(backupBasePath, fileName);

            // Get file size if possible
            long fileSize = 0;
            try
            {
                if (File.Exists(filePath))
                {
                    fileSize = new FileInfo(filePath).Length;
                }
            }
            catch
            {
                // Ignore file size errors
            }

            var backupInfo = new BackupInfo(
                BackupId: fileName,
                InstanceId: backupInstanceId,
                Description: null,
                CompressionFormat: compressionFormat,
                SizeInBytes: fileSize,
                CreatedAt: timestamp,
                FilePath: filePath,
                IsAutomatic: false,
                ServerVersion: null
            );

            backups.Add(backupInfo);
        }

        return Result<IReadOnlyList<BackupInfo>>.Success(backups);
    }

    private string ParseBackupCreationOutput(string output)
    {
        // Parse the backup creation output to extract backup ID
        // Expected format: "Backup created successfully: backup_server1_20250119_143022.tar.gz"

        var backupPattern = new Regex(
            @"backup_[a-zA-Z0-9_-]+_\d{8}_\d{6}\.tar\.(gz|zst)",
            RegexOptions.IgnoreCase
        );

        var match = backupPattern.Match(output);
        if (match.Success)
        {
            return match.Value;
        }

        // If pattern doesn't match, return empty string
        return string.Empty;
    }

    private InstanceStatus ParseStatusOutput(string instanceId, string output)
    {
        // Parse the status output from POK Manager
        // This is a basic implementation - will be enhanced as we learn the actual output format

        var state = InstanceState.Unknown;
        var health = ProcessHealth.Unknown;
        TimeSpan? uptime = null;
        var playerCount = 0;
        var maxPlayers = 0;
        string? version = null;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();

            // Parse status/state
            if (trimmedLine.Contains("running", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.Contains("Status: running", StringComparison.OrdinalIgnoreCase))
            {
                state = InstanceState.Running;
            }
            else if (trimmedLine.Contains("stopped", StringComparison.OrdinalIgnoreCase) ||
                     trimmedLine.Contains("Status: stopped", StringComparison.OrdinalIgnoreCase))
            {
                state = InstanceState.Stopped;
            }

            // Parse health
            if (trimmedLine.Contains("healthy", StringComparison.OrdinalIgnoreCase))
            {
                health = ProcessHealth.Healthy;
            }
            else if (trimmedLine.Contains("unhealthy", StringComparison.OrdinalIgnoreCase))
            {
                health = ProcessHealth.Unhealthy;
            }

            // Parse uptime (basic implementation)
            var uptimeMatch = Regex.Match(trimmedLine, @"Uptime:\s*(\d+)\s*(hour|minute)", RegexOptions.IgnoreCase);
            if (uptimeMatch.Success)
            {
                var value = int.Parse(uptimeMatch.Groups[1].Value);
                var unit = uptimeMatch.Groups[2].Value.ToLowerInvariant();
                uptime = unit.StartsWith("hour") ? TimeSpan.FromHours(value) : TimeSpan.FromMinutes(value);
            }
        }

        return new InstanceStatus(
            InstanceId: instanceId,
            State: state,
            Health: health,
            Uptime: uptime,
            PlayerCount: playerCount,
            MaxPlayers: maxPlayers,
            Version: version,
            LastUpdated: DateTimeOffset.UtcNow
        );
    }

    private InstanceDetails ParseDetailsOutput(string instanceId, string output)
    {
        // Parse the details output from POK Manager
        // This is a basic implementation - will be enhanced as we learn the actual output format

        var serverName = "Unknown";
        var state = InstanceState.Unknown;
        var health = ProcessHealth.Unknown;
        var port = 8211; // Default Palworld port
        var maxPlayers = 32; // Default
        var playerCount = 0;
        string? version = null;
        TimeSpan? uptime = null;
        var installPath = $"{_configuration.InstancesBasePath}/Instance_{instanceId}";
        var worldPath = $"{installPath}/Pal/Saved";
        var configPath = $"{installPath}/Pal/Saved/Config";

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();

            // Parse server name
            var serverNameMatch = Regex.Match(trimmedLine, @"ServerName:\s*(.+)", RegexOptions.IgnoreCase);
            if (serverNameMatch.Success)
            {
                serverName = serverNameMatch.Groups[1].Value.Trim();
            }

            // Parse port
            var portMatch = Regex.Match(trimmedLine, @"Port:\s*(\d+)", RegexOptions.IgnoreCase);
            if (portMatch.Success)
            {
                port = int.Parse(portMatch.Groups[1].Value);
            }

            // Parse max players
            var maxPlayersMatch = Regex.Match(trimmedLine, @"MaxPlayers:\s*(\d+)", RegexOptions.IgnoreCase);
            if (maxPlayersMatch.Success)
            {
                maxPlayers = int.Parse(maxPlayersMatch.Groups[1].Value);
            }

            // Parse version
            var versionMatch = Regex.Match(trimmedLine, @"Version:\s*(.+)", RegexOptions.IgnoreCase);
            if (versionMatch.Success)
            {
                version = versionMatch.Groups[1].Value.Trim();
            }

            // Parse paths
            var installPathMatch = Regex.Match(trimmedLine, @"InstallPath:\s*(.+)", RegexOptions.IgnoreCase);
            if (installPathMatch.Success)
            {
                installPath = installPathMatch.Groups[1].Value.Trim();
            }

            // Parse status for state
            if (trimmedLine.Contains("running", StringComparison.OrdinalIgnoreCase))
            {
                state = InstanceState.Running;
            }
            else if (trimmedLine.Contains("stopped", StringComparison.OrdinalIgnoreCase))
            {
                state = InstanceState.Stopped;
            }

            // Parse health
            if (trimmedLine.Contains("healthy", StringComparison.OrdinalIgnoreCase))
            {
                health = ProcessHealth.Healthy;
            }
        }

        return new InstanceDetails(
            InstanceId: instanceId,
            ServerName: serverName,
            State: state,
            Health: health,
            Port: port,
            MaxPlayers: maxPlayers,
            PlayerCount: playerCount,
            Version: version,
            Uptime: uptime,
            InstallPath: installPath,
            WorldPath: worldPath,
            ConfigPath: configPath,
            CreatedAt: DateTimeOffset.UtcNow, // TODO: Parse from actual data
            LastStartedAt: null,
            LastStoppedAt: null,
            Configuration: new Dictionary<string, string>() // TODO: Parse from actual data
        );
    }

    #endregion
}
