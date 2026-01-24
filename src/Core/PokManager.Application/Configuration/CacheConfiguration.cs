namespace PokManager.Application.Configuration;

/// <summary>
/// Configuration for cache TTL (Time To Live) settings for different data types.
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// TTL for instance status data (default: 5 seconds).
    /// </summary>
    public TimeSpan InstanceStatusTtl { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// TTL for instance details data (default: 30 seconds).
    /// </summary>
    public TimeSpan InstanceDetailsTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TTL for container metrics data (default: 10 seconds).
    /// </summary>
    public TimeSpan ContainerMetricsTtl { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// TTL for backup list data (default: 2 minutes).
    /// </summary>
    public TimeSpan BackupListTtl { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// TTL for configuration data (default: 5 minutes).
    /// </summary>
    public TimeSpan ConfigurationTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// TTL for player list data (default: 30 seconds).
    /// </summary>
    public TimeSpan PlayerListTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// TTL for server logs data (default: 15 seconds).
    /// </summary>
    public TimeSpan ServerLogsTtl { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// TTL for template list data (default: 10 minutes).
    /// </summary>
    public TimeSpan TemplateListTtl { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Default TTL for data without specific configuration (default: 1 minute).
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Maximum number of cache entries before eviction starts (default: 1000).
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;
}
