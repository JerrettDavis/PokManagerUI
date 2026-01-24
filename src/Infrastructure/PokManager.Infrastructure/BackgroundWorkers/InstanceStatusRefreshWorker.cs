using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Caching;
using PokManager.Application.Configuration;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that refreshes instance status data in the cache.
/// </summary>
public class InstanceStatusRefreshWorker : RefreshWorkerBase
{
    private readonly IPokManagerClient _pokManagerClient;

    public InstanceStatusRefreshWorker(
        IRefreshQueue refreshQueue,
        ICacheService cacheService,
        IPokManagerClient pokManagerClient,
        CacheConfiguration cacheConfig,
        ILogger<InstanceStatusRefreshWorker> logger)
        : base(refreshQueue, cacheService, logger, cacheConfig)
    {
        _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    }

    protected override async Task ProcessRefreshRequest(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (request.Type != RefreshType.InstanceStatus && request.Type != RefreshType.AllInstanceData)
        {
            return; // Not our responsibility
        }

        if (string.IsNullOrEmpty(request.InstanceId))
        {
            Logger.LogWarning("InstanceId is required for InstanceStatus refresh");
            return;
        }

        try
        {
            var statusResult = await _pokManagerClient.GetInstanceStatusAsync(
                request.InstanceId,
                cancellationToken
            );

            if (statusResult.IsSuccess)
            {
                await CacheService.SetAsync(
                    CacheKeys.InstanceStatus(request.InstanceId),
                    statusResult.Value,
                    CacheConfig.InstanceStatusTtl,
                    cancellationToken
                );

                Logger.LogDebug("Cached instance status for {InstanceId}", request.InstanceId);
            }
            else
            {
                Logger.LogWarning("Failed to fetch instance status for {InstanceId}: {Error}",
                    request.InstanceId, statusResult.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing instance status for {InstanceId}", request.InstanceId);
        }
    }
}
