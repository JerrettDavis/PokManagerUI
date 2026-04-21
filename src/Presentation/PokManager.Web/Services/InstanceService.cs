using System.Net.Http.Json;
using PokManager.Domain.Common;
using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// Facade service for instance operations, calling the API service via HTTP.
/// </summary>
public class InstanceService
{
    private readonly HttpClient _httpClient;

    public InstanceService(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Retrieves configuration for a specific instance from docker-compose file.
    /// </summary>
    public async Task<DockerComposeConfigDto?> GetInstanceConfigurationAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DockerComposeConfigDto>(
                $"/api/instances/{instanceId}/config",
                cancellationToken);

            return response;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Retrieves all instances with their current status.
    /// </summary>
    public async Task<Result<List<InstanceViewModel>>> GetAllInstancesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ListInstancesResponseDto>("/api/instances", cancellationToken);

            if (response?.Instances == null)
            {
                return Result.Failure<List<InstanceViewModel>>("Failed to retrieve instances from API");
            }

            // Fetch all instance details and configs in parallel
            var tasks = response.Instances.Select(async instance =>
            {
                try
                {
                    // Fetch status and config in parallel for each instance
                    var detailsTask = _httpClient.GetFromJsonAsync<InstanceStatusResponseDto>(
                        $"/api/instances/{instance.Id}/status",
                        cancellationToken);

                    var configTask = GetInstanceConfigurationAsync(instance.Id, cancellationToken);

                    await Task.WhenAll(detailsTask, configTask);

                    var detailsResponse = await detailsTask;
                    var config = await configTask;

                    if (detailsResponse?.Status != null)
                    {
                        var vm = MapStatusToViewModel(detailsResponse.Status, instance.Id);

                        if (config != null)
                        {
                            ApplyConfigurationToViewModel(vm, config);
                        }

                        return vm;
                    }

                    return null;
                }
                catch
                {
                    // Return null for failed instances, they'll be filtered out
                    return null;
                }
            }).ToList();

            var viewModels = (await Task.WhenAll(tasks))
                .Where(vm => vm != null)
                .Cast<InstanceViewModel>()
                .ToList();

            return Result<List<InstanceViewModel>>.Success(viewModels);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<InstanceViewModel>>($"Error loading instances: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves detailed status for a specific instance.
    /// </summary>
    public async Task<Result<InstanceViewModel>> GetInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<InstanceStatusResponseDto>(
                $"/api/instances/{instanceId}/status",
                cancellationToken);

            if (response?.Status == null)
            {
                return Result.Failure<InstanceViewModel>($"Instance '{instanceId}' not found");
            }

            return Result<InstanceViewModel>.Success(MapStatusToViewModel(response.Status, instanceId));
        }
        catch (Exception ex)
        {
            return Result.Failure<InstanceViewModel>($"Error loading instance: {ex.Message}");
        }
    }

    /// <summary>
    /// Starts an instance.
    /// </summary>
    public async Task<Result<string>> StartInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/instances/{instanceId}/start",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Success($"Instance '{instanceId}' started successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<string>($"Failed to start instance: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Error starting instance: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops an instance.
    /// </summary>
    public async Task<Result<string>> StopInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/instances/{instanceId}/stop",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Success($"Instance '{instanceId}' stopped successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<string>($"Failed to stop instance: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Error stopping instance: {ex.Message}");
        }
    }

    /// <summary>
    /// Restarts an instance.
    /// </summary>
    public async Task<Result<string>> RestartInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/instances/{instanceId}/restart",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Success($"Instance '{instanceId}' restarted successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<string>($"Failed to restart instance: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Error restarting instance: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves the world for an instance.
    /// </summary>
    public async Task<Result<string>> SaveWorldAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/instances/{instanceId}/save",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return Result<string>.Success($"World saved for instance '{instanceId}'");
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return Result.Failure<string>($"Failed to save world: {errorContent}");
        }
        catch (Exception ex)
        {
            return Result.Failure<string>($"Error saving world: {ex.Message}");
        }
    }

    // DTOs for API responses
    private record ListInstancesResponseDto(List<InstanceSummaryDto> Instances);

    public record DockerComposeConfigDto(
        string InstanceName,
        string ContainerName,
        string SessionName,
        string MapName,
        int Port,
        int RconPort,
        int MaxPlayers,
        string ServerPassword,
        string AdminPassword,
        bool BattleEyeEnabled,
        bool ApiEnabled,
        bool RconEnabled,
        string ClusterId,
        List<string> ModIds,
        List<string> PassiveMods,
        string Motd,
        int MotdDuration,
        string CustomServerArgs,
        bool UpdateServer,
        int CheckForUpdateInterval,
        string UpdateWindowMinimum,
        string UpdateWindowMaximum,
        int RestartNoticeMinutes,
        string TimeZone,
        string MemoryLimit,
        string ConfigFilePath
    );
    private record InstanceStatusResponseDto(InstanceStatusDto Status);

    private record InstanceSummaryDto(string Id, int State, int Health, DateTimeOffset? LastStartedAt, string? ContainerId);

    private record InstanceStatusDto(
        string Id,
        int State,
        DateTimeOffset LastCheckedAt,
        string? ContainerId,
        int Health,
        string? Uptime,
        int PlayerCount,
        int MaxPlayers,
        string? Version);

    private InstanceViewModel MapStatusToViewModel(InstanceStatusDto dto, string friendlyName)
    {
        TimeSpan? parsedUptime = null;
        if (!string.IsNullOrEmpty(dto.Uptime) && TimeSpan.TryParse(dto.Uptime, out var uptime))
        {
            parsedUptime = uptime;
        }

        return new InstanceViewModel
        {
            Id = dto.Id,
            Name = friendlyName ?? dto.Id,
            Status = MapInstanceState((InstanceState)dto.State),
            Health = MapProcessHealth((ProcessHealth)dto.Health),
            StartedAt = parsedUptime.HasValue
                ? DateTime.UtcNow - parsedUptime.Value
                : null,
            Uptime = parsedUptime.HasValue ? FormatUptime(parsedUptime.Value) : "N/A",
            CurrentPlayers = dto.PlayerCount,
            MaxPlayers = dto.MaxPlayers,
            Version = dto.Version ?? "Unknown",
            ServerMap = "Unknown", // Will be populated from instance details later
            Mods = new List<string>()
        };
    }

    private void ApplyConfigurationToViewModel(InstanceViewModel vm, DockerComposeConfigDto config)
    {
        vm.SessionName = config.SessionName;
        vm.ContainerName = config.ContainerName;
        vm.ServerMap = config.MapName;
        vm.Port = config.Port;
        vm.RconPort = config.RconPort;
        vm.MaxPlayers = config.MaxPlayers;
        vm.ServerPassword = config.ServerPassword;
        vm.AdminPassword = config.AdminPassword;
        vm.BattleEyeEnabled = config.BattleEyeEnabled;
        vm.ApiEnabled = config.ApiEnabled;
        vm.RconEnabled = config.RconEnabled;
        vm.ClusterId = config.ClusterId;
        vm.ModIds = config.ModIds;
        vm.PassiveMods = config.PassiveMods;
        vm.Motd = config.Motd;
        vm.MotdDuration = config.MotdDuration;
        vm.CustomServerArgs = config.CustomServerArgs;
        vm.UpdateServer = config.UpdateServer;
        vm.CheckForUpdateInterval = config.CheckForUpdateInterval;
        vm.UpdateWindowMinimum = config.UpdateWindowMinimum;
        vm.UpdateWindowMaximum = config.UpdateWindowMaximum;
        vm.RestartNoticeMinutes = config.RestartNoticeMinutes;
        vm.TimeZone = config.TimeZone;
        vm.MemoryLimit = config.MemoryLimit;
    }

    private static Models.InstanceStatus MapInstanceState(InstanceState state)
    {
        return state switch
        {
            InstanceState.Running => Models.InstanceStatus.Running,
            InstanceState.Stopped => Models.InstanceStatus.Stopped,
            InstanceState.Starting => Models.InstanceStatus.Starting,
            InstanceState.Stopping => Models.InstanceStatus.Stopping,
            InstanceState.Failed => Models.InstanceStatus.Error,
            _ => Models.InstanceStatus.Unknown
        };
    }

    private static Models.InstanceHealth MapProcessHealth(ProcessHealth health)
    {
        return health switch
        {
            ProcessHealth.Healthy => Models.InstanceHealth.Healthy,
            ProcessHealth.Degraded => Models.InstanceHealth.Degraded,
            ProcessHealth.Unhealthy => Models.InstanceHealth.Unhealthy,
            _ => Models.InstanceHealth.Unknown
        };
    }

    private static string FormatUptime(TimeSpan uptime)
    {
        if (uptime.TotalDays >= 1)
            return $"{(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m";
        if (uptime.TotalHours >= 1)
            return $"{(int)uptime.TotalHours}h {uptime.Minutes}m";
        if (uptime.TotalMinutes >= 1)
            return $"{(int)uptime.TotalMinutes}m";

        return $"{(int)uptime.TotalSeconds}s";
    }

    // Enum definitions for mapping
    private enum InstanceState
    {
        Unknown = 0,
        Stopped = 1,
        Starting = 2,
        Running = 3,
        Stopping = 4,
        Failed = 5,
        Created = 6,
        Restarting = 7
    }

    private enum ProcessHealth
    {
        Unknown = 0,
        Healthy = 1,
        Degraded = 2,
        Unhealthy = 3
    }
}
