using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Caching;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.Caching;

/// <summary>
/// Service for cache invalidation that removes cache entries and triggers immediate refresh.
/// </summary>
public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly ICacheService _cacheService;
    private readonly IRefreshQueue _refreshQueue;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        ICacheService cacheService,
        IRefreshQueue refreshQueue,
        ILogger<CacheInvalidationService> logger)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _refreshQueue = refreshQueue ?? throw new ArgumentNullException(nameof(refreshQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvalidateInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        // Remove all instance-related cache entries
        await _cacheService.RemoveByPatternAsync(CacheKeys.InstancePattern(instanceId), cancellationToken);

        // Enqueue priority refresh to repopulate cache immediately
        await _refreshQueue.EnqueueAsync(
            new RefreshRequest(RefreshType.AllInstanceData, instanceId, Priority: true),
            cancellationToken
        );

        _logger.LogInformation("Invalidated cache for instance {InstanceId}", instanceId);
    }

    public async Task InvalidateBackupsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByPatternAsync(CacheKeys.BackupPattern(instanceId), cancellationToken);

        await _refreshQueue.EnqueueAsync(
            new RefreshRequest(RefreshType.BackupList, instanceId, Priority: true),
            cancellationToken
        );

        _logger.LogInformation("Invalidated backup cache for instance {InstanceId}", instanceId);
    }

    public async Task InvalidateConfigurationAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveAsync(CacheKeys.Configuration(instanceId), cancellationToken);

        await _refreshQueue.EnqueueAsync(
            new RefreshRequest(RefreshType.Configuration, instanceId, Priority: true),
            cancellationToken
        );

        _logger.LogInformation("Invalidated configuration cache for instance {InstanceId}", instanceId);
    }

    public async Task InvalidateAllInstancesAsync(CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveByPatternAsync(CacheKeys.AllInstancesPattern(), cancellationToken);
        await _cacheService.RemoveAsync(CacheKeys.InstanceList(), cancellationToken);

        _logger.LogInformation("Invalidated all instances cache");
    }

    public async Task InvalidateTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await _cacheService.RemoveAsync(CacheKeys.TemplateList(), cancellationToken);

        await _refreshQueue.EnqueueAsync(
            new RefreshRequest(RefreshType.TemplateList, Priority: true),
            cancellationToken
        );

        _logger.LogInformation("Invalidated templates cache");
    }
}
