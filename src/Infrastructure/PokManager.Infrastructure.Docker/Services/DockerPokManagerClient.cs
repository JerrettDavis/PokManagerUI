using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.Docker.Services;

/// <summary>
/// Docker-based implementation of IPokManagerClient for ASA servers.
/// Queries Docker containers and their environment variables for instance information.
/// </summary>
public class DockerPokManagerClient : IPokManagerClient
{
    private readonly IDockerService _dockerService;

    public DockerPokManagerClient(IDockerService dockerService)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
    }

    #region Discovery & Query

    public async Task<Result<IReadOnlyList<string>>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = await _dockerService.ListContainersAsync(cancellationToken);
            var instanceIds = containers
                .Where(c => c.Name.StartsWith("asa_", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Name.Substring(4))
                .ToList();

            return Result<IReadOnlyList<string>>.Success(instanceIds.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<string>>($"Failed to list instances: {ex.Message}");
        }
    }

    public async Task<Result<InstanceStatus>> GetInstanceStatusAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var detailsResult = await GetInstanceDetailsAsync(instanceId, cancellationToken);
        if (detailsResult.IsFailure)
        {
            return Result.Failure<InstanceStatus>(detailsResult.Error);
        }

        var details = detailsResult.Value;
        var status = new InstanceStatus(
            details.InstanceId,
            details.State,
            details.Health,
            details.Uptime,
            details.PlayerCount,
            details.MaxPlayers,
            details.Version,
            DateTimeOffset.UtcNow
        );

        return Result<InstanceStatus>.Success(status);
    }

    public async Task<Result<InstanceDetails>> GetInstanceDetailsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var basePath = "/home/pokuser/asa_server";
            var instancePath = Path.Combine(basePath, $"Instance_{instanceId}");
            var dockerComposePath = Path.Combine(instancePath, $"docker-compose-{instanceId}.yaml");

            // 1. Check if instance directory exists on disk (PRIMARY check)
            if (!Directory.Exists(instancePath))
            {
                return Result.Failure<InstanceDetails>($"Instance directory not found: {instancePath}");
            }

            // 2. Determine container status and gather container info
            var containerName = $"asa_{instanceId}";
            var container = await _dockerService.GetContainerAsync(containerName, cancellationToken);
            var hasValidDockerCompose = File.Exists(dockerComposePath);

            ContainerStatus containerStatus;
            string? containerId = null;
            Dictionary<string, string> envVars = new();
            Dictionary<string, string> containerState = new();

            if (!hasValidDockerCompose)
            {
                containerStatus = ContainerStatus.MissingComposeFile;
            }
            else if (container == null)
            {
                containerStatus = ContainerStatus.NotCreated;
            }
            else
            {
                containerId = container.Id;
                containerStatus = MapDockerStateToContainerStatus(container.State);

                // Get container environment variables and state via docker inspect
                envVars = await GetContainerEnvironmentAsync(containerName, cancellationToken);
                containerState = await GetContainerStateAsync(containerName, cancellationToken);
            }

            // 3. Extract information from environment variables or use defaults
            var sessionName = envVars.GetValueOrDefault("SESSION_NAME", instanceId);
            var mapName = envVars.GetValueOrDefault("MAP_NAME", "Unknown");
            var maxPlayers = int.TryParse(envVars.GetValueOrDefault("MAX_PLAYERS", "32"), out var mp) ? mp : 32;
            var rconPort = int.TryParse(envVars.GetValueOrDefault("RCON_PORT", "27020"), out var rp) ? rp : 27020;
            var asaPort = int.TryParse(envVars.GetValueOrDefault("ASA_PORT", "7777"), out var ap) ? ap : 7777;

            // 4. Map container status to instance state
            var instanceState = MapContainerStatusToInstanceState(containerStatus);

            // 5. Calculate uptime (only if container is running)
            TimeSpan? uptime = null;
            if (containerStatus == ContainerStatus.Running &&
                containerState.ContainsKey("StartedAt") &&
                containerState["StartedAt"] != "0001-01-01T00:00:00Z")
            {
                if (DateTime.TryParse(containerState["StartedAt"], out var startedAt))
                {
                    uptime = DateTime.UtcNow - startedAt.ToUniversalTime();
                }
            }

            // 6. Determine health
            var health = DetermineHealthFromContainerStatus(containerStatus, instanceState, containerState);

            // 7. Get player count (for now return 0, will implement RCON query later)
            var playerCount = 0;

            // 8. Read configuration from GameUserSettings.ini
            var configPath = Path.Combine(instancePath, "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            var configuration = await ReadGameUserSettingsAsync(configPath, cancellationToken);

            // 9. Override with environment variable data (these always take precedence over defaults/INI values)
            configuration["SessionName"] = sessionName;
            configuration["MaxPlayers"] = maxPlayers.ToString();
            configuration["ServerMap"] = mapName;
            configuration["MapName"] = mapName;
            configuration["RCONPort"] = rconPort.ToString();

            // 10. Get directory creation time
            var dirInfo = new DirectoryInfo(instancePath);
            var createdAt = dirInfo.CreationTimeUtc;

            // 11. Build instance details with new fields
            var details = new InstanceDetails(
                instanceId,
                sessionName,
                instanceState,
                health,
                asaPort,
                maxPlayers,
                playerCount,
                null, // Version - could extract from container image tag if needed
                uptime,
                instancePath,
                Path.Combine(instancePath, "ShooterGame", "Saved"),
                Path.Combine(instancePath, "Saved", "Config"),
                createdAt,
                containerState.ContainsKey("StartedAt") && DateTime.TryParse(containerState["StartedAt"], out var sa)
                    ? (DateTimeOffset?)sa.ToUniversalTime()
                    : null,
                null, // LastStoppedAt - would need to get from container metadata
                configuration,
                // NEW FIELDS
                ContainerStatus: containerStatus,
                ContainerId: containerId,
                DockerComposeFilePath: hasValidDockerCompose ? dockerComposePath : null,
                HasValidDockerCompose: hasValidDockerCompose
            );

            return Result<InstanceDetails>.Success(details);
        }
        catch (Exception ex)
        {
            return Result.Failure<InstanceDetails>($"Failed to get instance details: {ex.Message}");
        }
    }

    #endregion

    #region Lifecycle Management

    public async Task<Result<Unit>> StartInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"asa_{instanceId}";
            var success = await _dockerService.StartContainerAsync(containerName, cancellationToken);
            
            return success 
                ? Result.Success() 
                : Result.Failure<Unit>($"Failed to start instance '{instanceId}'");
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to start instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> StopInstanceAsync(string instanceId, StopInstanceOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"asa_{instanceId}";
            var success = await _dockerService.StopContainerAsync(containerName, cancellationToken);
            
            return success 
                ? Result.Success() 
                : Result.Failure<Unit>($"Failed to stop instance '{instanceId}'");
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to stop instance: {ex.Message}");
        }
    }

    public async Task<Result<Unit>> RestartInstanceAsync(string instanceId, RestartInstanceOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"asa_{instanceId}";
            var success = await _dockerService.RestartContainerAsync(containerName, cancellationToken);
            
            return success 
                ? Result.Success() 
                : Result.Failure<Unit>($"Failed to restart instance '{instanceId}'");
        }
        catch (Exception ex)
        {
            return Result.Failure<Unit>($"Failed to restart instance: {ex.Message}");
        }
    }

    public Task<Result<string>> CreateInstanceAsync(CreateInstanceRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<string>("Create instance is not implemented via Docker client"));
    }

    public Task<Result<Unit>> DeleteInstanceAsync(string instanceId, bool deleteBackups = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>("Delete instance is not implemented via Docker client"));
    }

    #endregion

    #region Helper Methods

    private async Task<Dictionary<string, string>> GetContainerEnvironmentAsync(string containerName, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"inspect {containerName} --format \"{{{{json .Config.Env}}}}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var envVars = new Dictionary<string, string>();
        
        try
        {
            var envArray = JsonSerializer.Deserialize<string[]>(output);
            if (envArray != null)
            {
                foreach (var env in envArray)
                {
                    var parts = env.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        envVars[parts[0]] = parts[1];
                    }
                }
            }
        }
        catch
        {
            // If parsing fails, return empty dictionary
        }

        return envVars;
    }

    private async Task<Dictionary<string, string>> GetContainerStateAsync(string containerName, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"inspect {containerName} --format \"{{{{json .State}}}}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        var state = new Dictionary<string, string>();
        
        try
        {
            var stateJson = JsonSerializer.Deserialize<JsonElement>(output);
            state["Status"] = stateJson.GetProperty("Status").GetString() ?? "unknown";
            state["Running"] = stateJson.GetProperty("Running").GetBoolean().ToString();
            state["StartedAt"] = stateJson.GetProperty("StartedAt").GetString() ?? "";
            state["FinishedAt"] = stateJson.GetProperty("FinishedAt").GetString() ?? "";
        }
        catch
        {
            // If parsing fails, return default state
            state["Status"] = "unknown";
            state["Running"] = "false";
        }

        return state;
    }

    private InstanceState MapContainerStateToInstanceState(Dictionary<string, string> containerState)
    {
        var status = containerState.GetValueOrDefault("Status", "unknown").ToLowerInvariant();
        var running = containerState.GetValueOrDefault("Running", "false").ToLowerInvariant() == "true";

        return status switch
        {
            "running" when running => InstanceState.Running,
            "exited" => InstanceState.Stopped,
            "created" => InstanceState.Created,
            "restarting" => InstanceState.Restarting,
            "paused" => InstanceState.Stopped,
            "dead" => InstanceState.Failed,
            _ => InstanceState.Unknown
        };
    }

    private ContainerStatus MapDockerStateToContainerStatus(string dockerState)
    {
        return dockerState.ToLowerInvariant() switch
        {
            "running" => ContainerStatus.Running,
            "exited" => ContainerStatus.Stopped,
            "created" => ContainerStatus.Stopped,
            "restarting" => ContainerStatus.Restarting,
            "paused" => ContainerStatus.Paused,
            "dead" => ContainerStatus.Dead,
            _ => ContainerStatus.Unknown
        };
    }

    private InstanceState MapContainerStatusToInstanceState(ContainerStatus containerStatus)
    {
        return containerStatus switch
        {
            ContainerStatus.Running => InstanceState.Running,
            ContainerStatus.Stopped => InstanceState.Stopped,
            ContainerStatus.Restarting => InstanceState.Restarting,
            ContainerStatus.Dead => InstanceState.Failed,
            ContainerStatus.NotCreated => InstanceState.Stopped, // Data exists, no container
            ContainerStatus.MissingComposeFile => InstanceState.Unknown,
            _ => InstanceState.Unknown
        };
    }

    private ProcessHealth DetermineHealthFromContainerStatus(
        ContainerStatus containerStatus,
        InstanceState instanceState,
        Dictionary<string, string> containerState)
    {
        // If container doesn't exist or is in a bad state, health is unknown
        if (containerStatus == ContainerStatus.NotCreated ||
            containerStatus == ContainerStatus.MissingComposeFile ||
            containerStatus == ContainerStatus.Dead)
        {
            return ProcessHealth.Unknown;
        }

        // If not running, health is unknown
        if (instanceState != InstanceState.Running)
        {
            return ProcessHealth.Unknown;
        }

        // For now, assume healthy if running
        // Could be enhanced to check container health checks or query RCON
        return ProcessHealth.Healthy;
    }

    private ProcessHealth DetermineHealth(Dictionary<string, string> containerState, InstanceState instanceState)
    {
        // If not running, health is unknown
        if (instanceState != InstanceState.Running)
        {
            return ProcessHealth.Unknown;
        }

        // For now, assume healthy if running
        // Could be enhanced to check container health checks or query RCON
        return ProcessHealth.Healthy;
    }

    private async Task<Dictionary<string, string>> ReadGameUserSettingsAsync(string configPath, CancellationToken cancellationToken)
    {
        // Start with ARK configuration defaults - this ensures all settings are visible even when using defaults
        var defaults = Application.Configuration.ArkConfigurationDefaults.GetDefaults();
        var configuration = new Dictionary<string, string>();

        // Add all defaults to the configuration
        foreach (var kvp in defaults)
        {
            configuration[kvp.Key] = kvp.Value.DefaultValue;
        }

        try
        {
            if (!File.Exists(configPath))
            {
                return configuration;
            }

            var lines = await File.ReadAllLinesAsync(configPath, cancellationToken);
            bool inServerSettings = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Check if we're entering the [ServerSettings] section
                if (trimmedLine.Equals("[ServerSettings]", StringComparison.OrdinalIgnoreCase))
                {
                    inServerSettings = true;
                    continue;
                }

                // Check if we're entering a different section
                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    inServerSettings = false;
                    continue;
                }

                // Skip comments and empty lines
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                // Parse key-value pairs only in [ServerSettings] section
                if (inServerSettings && trimmedLine.Contains('='))
                {
                    var parts = trimmedLine.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // Override default value with actual value from INI file
                        configuration[key] = value;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If we can't read the config file, return defaults
            // Logging would be helpful here but keeping it simple for now
        }

        return configuration;
    }

    #endregion

    #region Not Implemented Methods

    public Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<IReadOnlyList<BackupInfo>>("Backup operations not implemented via Docker client"));
    }

    public Task<Result<string>> CreateBackupAsync(string instanceId, CreateBackupOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<string>("Backup operations not implemented via Docker client"));
    }

    public Task<Result<Unit>> RestoreBackupAsync(string instanceId, string backupId, RestoreBackupOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>("Backup operations not implemented via Docker client"));
    }

    public Task<Result<Stream>> DownloadBackupAsync(string instanceId, string backupId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Stream>("Backup operations not implemented via Docker client"));
    }

    public Task<Result<Unit>> DeleteBackupAsync(string instanceId, string backupId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>("Backup operations not implemented via Docker client"));
    }

    public Task<Result<UpdateAvailability>> CheckForUpdatesAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<UpdateAvailability>("Update operations not implemented via Docker client"));
    }

    public Task<Result<UpdateResult>> ApplyUpdatesAsync(string instanceId, ApplyUpdatesOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<UpdateResult>("Update operations not implemented via Docker client"));
    }

    public Task<Result<IReadOnlyDictionary<string, string>>> GetConfigurationAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<IReadOnlyDictionary<string, string>>("Configuration operations not implemented via Docker client"));
    }

    public Task<Result<ConfigurationValidationResult>> ValidateConfigurationAsync(string instanceId, IReadOnlyDictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<ConfigurationValidationResult>("Configuration operations not implemented via Docker client"));
    }

    public Task<Result<ApplyConfigurationResult>> ApplyConfigurationAsync(string instanceId, IReadOnlyDictionary<string, string> configuration, ApplyConfigurationOptions? options = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<ApplyConfigurationResult>("Configuration operations not implemented via Docker client"));
    }

    public async Task<Result<IReadOnlyList<LogEntry>>> GetLogsAsync(string instanceId, GetLogsOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerName = $"asa_{instanceId}";
            var lines = options?.MaxLines ?? 100;
            var logs = await _dockerService.GetContainerLogsAsync(containerName, lines, cancellationToken);
            
            // Parse logs into LogEntry objects (simple implementation for now)
            var logEntries = logs.Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => new LogEntry(
                    DateTimeOffset.UtcNow,
                    LogLevel.Information,
                    line,
                    "docker",
                    instanceId,
                    null,
                    null
                ))
                .ToList();

            return Result<IReadOnlyList<LogEntry>>.Success(logEntries.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<LogEntry>>($"Failed to get logs: {ex.Message}");
        }
    }

    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(string instanceId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Simple implementation - in reality would use docker logs -f
        var logsResult = await GetLogsAsync(instanceId, new GetLogsOptions(MaxLines: 100), cancellationToken);
        if (logsResult.IsSuccess)
        {
            foreach (var log in logsResult.Value)
            {
                yield return log;
            }
        }
    }

    public Task<Result<HealthCheckResult>> HealthCheckAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<HealthCheckResult>("Health check not implemented via Docker client"));
    }

    public Task<Result<Unit>> SendChatMessageAsync(string instanceId, string message, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>("Chat message not implemented via Docker client"));
    }

    public Task<Result<Unit>> SaveWorldAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<Unit>("Save world not implemented via Docker client"));
    }

    public Task<Result<string>> ExecuteCustomCommandAsync(string instanceId, string command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Failure<string>("Custom command not implemented via Docker client"));
    }

    #endregion
}
