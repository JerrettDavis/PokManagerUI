using Microsoft.Extensions.Logging;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Infrastructure.Docker.Models;

namespace PokManager.Infrastructure.Docker.Services;

/// <summary>
/// Discovers ARK server instances by scanning the filesystem for Instance_* directories.
/// This is the PRIMARY discovery mechanism. Docker container status is queried separately.
/// </summary>
public class DiskBasedInstanceDiscoveryService : IInstanceDiscoveryService
{
    private readonly string _basePath;
    private readonly ILogger<DiskBasedInstanceDiscoveryService> _logger;
    private List<string>? _cachedInstances;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheLifetime;

    public DiskBasedInstanceDiscoveryService(
        ILogger<DiskBasedInstanceDiscoveryService> logger,
        string basePath = "/home/pokuser/asa_server",
        TimeSpan? cacheLifetime = null)
    {
        _logger = logger;
        _basePath = basePath;
        _cacheLifetime = cacheLifetime ?? TimeSpan.FromMinutes(5);
    }

    public async Task<Result<IReadOnlyList<string>>> DiscoverInstancesAsync(
        CancellationToken cancellationToken = default)
    {
        // Return cached results if still valid
        if (_cachedInstances != null && DateTime.UtcNow < _cacheExpiry)
        {
            _logger.LogDebug("Returning cached instance list with {Count} instances", _cachedInstances.Count);
            return Result<IReadOnlyList<string>>.Success(_cachedInstances);
        }

        try
        {
            var instanceIds = new List<string>();

            if (!Directory.Exists(_basePath))
            {
                _logger.LogWarning("Base path '{BasePath}' does not exist", _basePath);
                return Result.Failure<IReadOnlyList<string>>(
                    $"Base path '{_basePath}' does not exist");
            }

            // Scan for Instance_* directories
            var directories = Directory.GetDirectories(_basePath, "Instance_*");

            _logger.LogDebug("Found {Count} Instance_* directories in {BasePath}", directories.Length, _basePath);

            foreach (var dir in directories)
            {
                var dirName = Path.GetFileName(dir);
                if (dirName.StartsWith("Instance_", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract instance ID from "Instance_TwinPeak" -> "TwinPeak"
                    var instanceId = dirName.Substring(9);
                    if (!string.IsNullOrWhiteSpace(instanceId))
                    {
                        instanceIds.Add(instanceId);
                        _logger.LogTrace("Discovered instance: {InstanceId}", instanceId);
                    }
                }
            }

            // Update cache
            _cachedInstances = instanceIds;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);

            _logger.LogInformation("Discovered {Count} instances from disk", instanceIds.Count);
            return Result<IReadOnlyList<string>>.Success(instanceIds.AsReadOnly());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover instances from disk at {BasePath}", _basePath);
            return Result.Failure<IReadOnlyList<string>>(
                $"Failed to discover instances from disk: {ex.Message}");
        }
    }

    public Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        _cachedInstances = null;
        _cacheExpiry = DateTime.MinValue;
        _logger.LogDebug("Instance discovery cache invalidated");
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var result = await DiscoverInstancesAsync(cancellationToken);
        return result.IsSuccess &&
               result.Value.Any(id => id.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets detailed disk information about an instance.
    /// </summary>
    public async Task<Result<InstanceDiskInfo>> GetInstanceDiskInfoAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var instancePath = Path.Combine(_basePath, $"Instance_{instanceId}");

            if (!Directory.Exists(instancePath))
            {
                _logger.LogWarning("Instance directory not found: {InstancePath}", instancePath);
                return Result.Failure<InstanceDiskInfo>(
                    $"Instance directory not found: {instancePath}");
            }

            var dockerComposePath = Path.Combine(instancePath, $"docker-compose-{instanceId}.yaml");
            var gameUserSettingsPath = Path.Combine(instancePath,
                "Saved", "Config", "WindowsServer", "GameUserSettings.ini");
            var savedDataPath = Path.Combine(instancePath, "ShooterGame", "Saved");

            var dirInfo = new DirectoryInfo(instancePath);
            var dirSize = await CalculateDirectorySizeAsync(instancePath, cancellationToken);

            var diskInfo = new InstanceDiskInfo
            {
                InstanceId = instanceId,
                DirectoryPath = instancePath,
                DockerComposeFilePath = File.Exists(dockerComposePath) ? dockerComposePath : null,
                HasGameUserSettings = File.Exists(gameUserSettingsPath),
                HasSavedData = Directory.Exists(savedDataPath),
                DirectoryCreated = dirInfo.CreationTimeUtc,
                DirectorySizeBytes = dirSize
            };

            _logger.LogDebug("Retrieved disk info for instance {InstanceId}: {DirectorySize} bytes",
                instanceId, dirSize);

            return Result<InstanceDiskInfo>.Success(diskInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disk info for instance '{InstanceId}'", instanceId);
            return Result.Failure<InstanceDiskInfo>(
                $"Failed to get disk info for instance '{instanceId}': {ex.Message}");
        }
    }

    private async Task<long> CalculateDirectorySizeAsync(
        string path,
        CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            try
            {
                var dirInfo = new DirectoryInfo(path);
                return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                    .Sum(file => file.Length);
            }
            catch
            {
                return 0; // Return 0 if calculation fails
            }
        }, cancellationToken);
    }
}
