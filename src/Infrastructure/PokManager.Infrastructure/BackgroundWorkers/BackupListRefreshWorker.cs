using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Caching;
using PokManager.Application.Configuration;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that refreshes backup list data in the cache.
/// </summary>
public class BackupListRefreshWorker : RefreshWorkerBase
{
    private readonly IPokManagerClient _pokManagerClient;

    public BackupListRefreshWorker(
        IRefreshQueue refreshQueue,
        ICacheService cacheService,
        IPokManagerClient pokManagerClient,
        CacheConfiguration cacheConfig,
        ILogger<BackupListRefreshWorker> logger)
        : base(refreshQueue, cacheService, logger, cacheConfig)
    {
        _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
    }

    protected override async Task ProcessRefreshRequest(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (request.Type != RefreshType.BackupList && request.Type != RefreshType.AllInstanceData)
        {
            return; // Not our responsibility
        }

        if (string.IsNullOrEmpty(request.InstanceId))
        {
            Logger.LogWarning("InstanceId is required for BackupList refresh");
            return;
        }

        try
        {
            var backupsResult = await _pokManagerClient.ListBackupsAsync(
                request.InstanceId,
                cancellationToken
            );

            if (backupsResult.IsSuccess)
            {
                await CacheService.SetAsync(
                    CacheKeys.BackupList(request.InstanceId),
                    backupsResult.Value,
                    CacheConfig.BackupListTtl,
                    cancellationToken
                );

                Logger.LogDebug("Cached backup list for {InstanceId}", request.InstanceId);
            }
            else
            {
                Logger.LogWarning("Failed to fetch backup list for {InstanceId}: {Error}",
                    request.InstanceId, backupsResult.Error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing backup list for {InstanceId}", request.InstanceId);
        }
    }
}
