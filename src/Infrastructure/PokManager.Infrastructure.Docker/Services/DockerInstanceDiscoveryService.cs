using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.Docker.Services;

/// <summary>
/// Docker-based implementation of IInstanceDiscoveryService.
/// Discovers ASA server instances by finding Docker containers with 'asa_' prefix.
/// </summary>
public class DockerInstanceDiscoveryService : IInstanceDiscoveryService
{
    private readonly IDockerService _dockerService;
    private List<string>? _cachedInstances;
    private DateTime _cacheExpiry = DateTime.MinValue;
    private readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);

    public DockerInstanceDiscoveryService(IDockerService dockerService)
    {
        _dockerService = dockerService ?? throw new ArgumentNullException(nameof(dockerService));
    }

    public async Task<Result<IReadOnlyList<string>>> DiscoverInstancesAsync(CancellationToken cancellationToken = default)
    {
        // Return cached results if still valid
        if (_cachedInstances != null && DateTime.UtcNow < _cacheExpiry)
        {
            return Result<IReadOnlyList<string>>.Success(_cachedInstances);
        }

        try
        {
            var containers = await _dockerService.ListContainersAsync(cancellationToken);

            // Extract instance IDs from container names (remove 'asa_' prefix)
            var instanceIds = containers
                .Where(c => c.Name.StartsWith("asa_", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Name.Substring(4)) // Remove 'asa_' prefix
                .ToList();

            // Update cache
            _cachedInstances = instanceIds;
            _cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);

            return Result<IReadOnlyList<string>>.Success(instanceIds.AsReadOnly());
        }
        catch (Exception ex)
        {
            return Result.Failure<IReadOnlyList<string>>($"Failed to discover instances: {ex.Message}");
        }
    }

    public Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        _cachedInstances = null;
        _cacheExpiry = DateTime.MinValue;
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var result = await DiscoverInstancesAsync(cancellationToken);
        if (result.IsFailure)
        {
            return false;
        }

        return result.Value.Any(id => id.Equals(instanceId, StringComparison.OrdinalIgnoreCase));
    }
}
