namespace PokManager.Application.Caching;

/// <summary>
/// Centralized cache key builder for consistent cache key generation.
/// </summary>
public static class CacheKeys
{
    // Instance-related keys
    public static string InstanceStatus(string instanceId) => $"instance:status:{instanceId}";
    public static string InstanceDetails(string instanceId) => $"instance:details:{instanceId}";
    public static string InstanceList() => "instance:list";

    // Container-related keys
    public static string ContainerMetrics(string instanceId) => $"container:metrics:{instanceId}";
    public static string ContainerLogs(string instanceId, int lines) => $"container:logs:{instanceId}:{lines}";

    // Backup-related keys
    public static string BackupList(string instanceId) => $"backup:list:{instanceId}";
    public static string BackupDetails(string instanceId, string backupId) => $"backup:details:{instanceId}:{backupId}";

    // Configuration-related keys
    public static string Configuration(string instanceId) => $"config:{instanceId}";

    // Player-related keys
    public static string PlayerList(string instanceId) => $"player:list:{instanceId}";

    // Template-related keys
    public static string TemplateList() => "template:list";
    public static string TemplateDetails(string templateId) => $"template:details:{templateId}";

    // Pattern invalidation helpers
    public static string InstancePattern(string instanceId) => $"instance:*:{instanceId}";
    public static string BackupPattern(string instanceId) => $"backup:*:{instanceId}";
    public static string AllInstancesPattern() => "instance:*";
}
