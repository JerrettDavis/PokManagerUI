using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Background worker that periodically enqueues refresh requests on intervals.
/// This replaces the polling logic from InstanceDataWorker.
/// </summary>
public class ScheduledRefreshWorker : BackgroundService
{
    private readonly IRefreshQueue _refreshQueue;
    private readonly IPokManagerClient _pokManagerClient;
    private readonly ILogger<ScheduledRefreshWorker> _logger;

    public ScheduledRefreshWorker(
        IRefreshQueue refreshQueue,
        IPokManagerClient pokManagerClient,
        ILogger<ScheduledRefreshWorker> logger)
    {
        _refreshQueue = refreshQueue ?? throw new ArgumentNullException(nameof(refreshQueue));
        _pokManagerClient = pokManagerClient ?? throw new ArgumentNullException(nameof(pokManagerClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledRefreshWorker started");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // Adjust interval as needed

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                // Get all instances
                var instancesResult = await _pokManagerClient.ListInstancesAsync(stoppingToken);
                if (instancesResult.IsSuccess)
                {
                    var instances = instancesResult.Value;

                    // Enqueue refresh requests for each instance
                    var requests = new List<RefreshRequest>();

                    foreach (var instanceId in instances)
                    {
                        // High-frequency data (every 30s)
                        requests.Add(new RefreshRequest(RefreshType.InstanceStatus, instanceId));

                        // Low-frequency data (every 2 minutes) - use modulo to reduce frequency
                        if (DateTime.UtcNow.Minute % 2 == 0 && DateTime.UtcNow.Second < 30)
                        {
                            requests.Add(new RefreshRequest(RefreshType.BackupList, instanceId));
                        }
                    }

                    await _refreshQueue.EnqueueBatchAsync(requests, stoppingToken);
                    _logger.LogDebug("Enqueued {Count} scheduled refresh requests", requests.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ScheduledRefreshWorker");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        _logger.LogInformation("ScheduledRefreshWorker stopped");
    }
}
