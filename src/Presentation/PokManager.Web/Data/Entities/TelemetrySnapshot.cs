namespace PokManager.Web.Data.Entities;

/// <summary>
/// Represents telemetry data at a specific point in time.
/// </summary>
public class TelemetrySnapshot
{
    public int Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    // Performance metrics
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }
    public long NetworkInKBps { get; set; }
    public long NetworkOutKBps { get; set; }
    public int Fps { get; set; }
    public int TickRate { get; set; }

    // Game stats (stored as JSON)
    public string? GameStatsJson { get; set; }

    // Navigation
    public int? InstanceSnapshotId { get; set; }
    public InstanceSnapshot? InstanceSnapshot { get; set; }
}
