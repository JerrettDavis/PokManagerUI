using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.Tests.Fakes;

/// <summary>
/// In-memory instance discovery service for testing.
/// </summary>
public class InMemoryInstanceDiscoveryService : IInstanceDiscoveryService
{
    private readonly List<string> _instances = new();
    private bool _cacheValid = true;

    public Task<Result<IReadOnlyList<string>>> DiscoverInstancesAsync(CancellationToken ct = default)
    {
        if (!_cacheValid)
        {
            // Simulate cache invalidation behavior
            _cacheValid = true;
        }

        return Task.FromResult(Result<IReadOnlyList<string>>.Success(_instances.AsReadOnly()));
    }

    public Task InvalidateCacheAsync(CancellationToken ct = default)
    {
        _cacheValid = false;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string instanceId, CancellationToken ct = default)
    {
        return Task.FromResult(_instances.Contains(instanceId));
    }

    /// <summary>
    /// Add an instance to the discovery service for testing.
    /// </summary>
    public void AddInstance(string instanceId)
    {
        if (!_instances.Contains(instanceId))
            _instances.Add(instanceId);
    }

    /// <summary>
    /// Remove an instance from the discovery service for testing.
    /// </summary>
    public void RemoveInstance(string instanceId)
    {
        _instances.Remove(instanceId);
    }

    /// <summary>
    /// Clear all instances for test isolation.
    /// </summary>
    public void Reset()
    {
        _instances.Clear();
        _cacheValid = true;
    }

    /// <summary>
    /// Check if cache is currently valid.
    /// </summary>
    public bool IsCacheValid() => _cacheValid;
}
