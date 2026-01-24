using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Configuration;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Base class for all refresh workers that process refresh requests from the queue.
/// </summary>
public abstract class RefreshWorkerBase : BackgroundService
{
    protected readonly IRefreshQueue RefreshQueue;
    protected readonly ICacheService CacheService;
    protected readonly ILogger Logger;
    protected readonly CacheConfiguration CacheConfig;

    protected RefreshWorkerBase(
        IRefreshQueue refreshQueue,
        ICacheService cacheService,
        ILogger logger,
        CacheConfiguration cacheConfig)
    {
        RefreshQueue = refreshQueue ?? throw new ArgumentNullException(nameof(refreshQueue));
        CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        CacheConfig = cacheConfig ?? throw new ArgumentNullException(nameof(cacheConfig));
    }

    /// <summary>
    /// Processes a single refresh request. Implementations should check the request type
    /// and only process requests they are responsible for.
    /// </summary>
    protected abstract Task ProcessRefreshRequest(RefreshRequest request, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("{WorkerName} started", GetType().Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await RefreshQueue.DequeueAsync(stoppingToken);
                if (request != null)
                {
                    await ProcessRefreshRequest(request, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "{WorkerName} encountered error processing refresh request", GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        Logger.LogInformation("{WorkerName} stopped", GetType().Name);
    }
}
