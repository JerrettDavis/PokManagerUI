using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.BackgroundWorkers;

/// <summary>
/// Channel-based implementation of the refresh queue for coordinating cache refresh requests.
/// </summary>
public class RefreshQueue : IRefreshQueue
{
    private readonly Channel<RefreshRequest> _channel;
    private readonly ILogger<RefreshQueue> _logger;

    public RefreshQueue(ILogger<RefreshQueue> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _channel = Channel.CreateUnbounded<RefreshRequest>(new UnboundedChannelOptions
        {
            SingleReader = false, // Multiple workers can read
            SingleWriter = false  // Multiple sources can write
        });
    }

    public async Task EnqueueAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(request, cancellationToken);
        _logger.LogDebug("Enqueued refresh request: {Type} for {InstanceId}",
            request.Type, request.InstanceId ?? "all");
    }

    public async Task<RefreshRequest?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _channel.Reader.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }

    public async Task EnqueueBatchAsync(IEnumerable<RefreshRequest> requests, CancellationToken cancellationToken = default)
    {
        foreach (var request in requests)
        {
            await _channel.Writer.WriteAsync(request, cancellationToken);
        }
    }

    public int GetQueueDepth()
    {
        return _channel.Reader.Count;
    }
}
