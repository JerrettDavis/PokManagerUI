using PokManager.Application.Ports;
using PokManager.Application.BackgroundWorkers;

namespace PokManager.ApiService.Endpoints;

/// <summary>
/// Endpoints for cache monitoring and management.
/// </summary>
public static class CacheEndpoints
{
    public static void MapCacheEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/cache").WithTags("Cache");

        // GET /api/cache/stats - Get cache statistics
        group.MapGet("/stats", async (
            IRefreshQueue refreshQueue,
            CancellationToken ct) =>
        {
            var stats = new
            {
                QueueDepth = refreshQueue.GetQueueDepth(),
                Timestamp = DateTimeOffset.UtcNow
            };

            return Results.Ok(stats);
        })
        .WithName("GetCacheStats")
        .WithOpenApi();

        // POST /api/cache/clear - Clear all cache
        group.MapPost("/clear", async (
            ICacheInvalidationService invalidation,
            CancellationToken ct) =>
        {
            await invalidation.InvalidateAllInstancesAsync(ct);
            await invalidation.InvalidateTemplatesAsync(ct);

            return Results.Ok(new { message = "All caches cleared successfully" });
        })
        .WithName("ClearAllCache")
        .WithOpenApi();

        // POST /api/cache/clear/{instanceId} - Clear cache for specific instance
        group.MapPost("/clear/{instanceId}", async (
            string instanceId,
            ICacheInvalidationService invalidation,
            CancellationToken ct) =>
        {
            await invalidation.InvalidateInstanceAsync(instanceId, ct);

            return Results.Ok(new { message = $"Cache cleared successfully for instance {instanceId}" });
        })
        .WithName("ClearInstanceCache")
        .WithOpenApi();
    }
}
