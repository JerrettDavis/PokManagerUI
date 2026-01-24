using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PokManager.Application.Configuration;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation using ConcurrentDictionary with TTL support.
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly CacheConfiguration _config;
    private readonly ILogger<InMemoryCacheService> _logger;

    private class CacheEntry
    {
        public object Value { get; set; } = null!;
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public InMemoryCacheService(
        CacheConfiguration config,
        ILogger<InMemoryCacheService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTimeOffset.UtcNow < entry.ExpiresAt)
            {
                _logger.LogDebug("Cache hit for key: {Key}", key);
                return Task.FromResult(entry.Value as T);
            }

            // Expired, remove it
            _cache.TryRemove(key, out _);
            _logger.LogDebug("Cache expired for key: {Key}", key);
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default) where T : class
    {
        var effectiveTtl = ttl ?? _config.DefaultTtl;
        var entry = new CacheEntry
        {
            Value = value,
            ExpiresAt = DateTimeOffset.UtcNow.Add(effectiveTtl)
        };

        _cache[key] = entry;
        _logger.LogDebug("Cached value for key: {Key} with TTL: {Ttl}", key, effectiveTtl);

        // Enforce max size
        if (_cache.Count > _config.MaxCacheSize)
        {
            EvictOldestEntries();
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default) where T : class
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, ttl, cancellationToken);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        _logger.LogDebug("Removed cache entry for key: {Key}", key);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var regex = new Regex(WildcardToRegex(pattern));
        var keysToRemove = _cache.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogDebug("Removed {Count} cache entries matching pattern: {Pattern}", keysToRemove.Count, pattern);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            return Task.FromResult(DateTimeOffset.UtcNow < entry.ExpiresAt);
        }
        return Task.FromResult(false);
    }

    private void EvictOldestEntries()
    {
        var toRemove = _cache
            .OrderBy(kvp => kvp.Value.ExpiresAt)
            .Take(_cache.Count - (_config.MaxCacheSize * 9 / 10)) // Remove 10%
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.TryRemove(key, out _);
        }

        _logger.LogInformation("Evicted {Count} oldest cache entries to enforce max size", toRemove.Count);
    }

    private static string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
    }
}
