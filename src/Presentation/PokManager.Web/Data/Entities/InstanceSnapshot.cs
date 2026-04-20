namespace PokManager.Web.Data.Entities;

/// <summary>
/// Represents a snapshot of instance state at a specific point in time.
/// </summary>
public class InstanceSnapshot
{
    public int Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    // Status fields
    public string Status { get; set; } = string.Empty;
    public string Health { get; set; } = string.Empty;
    public string? Uptime { get; set; }
    public DateTime? StartedAt { get; set; }

    // Server info
    public string? ServerMap { get; set; }
    public int MaxPlayers { get; set; }
    public int CurrentPlayers { get; set; }
    public int Port { get; set; }
    public string? Version { get; set; }
    public bool IsPublic { get; set; }
    public bool IsPvE { get; set; }

    // Resource usage
    public double CpuUsagePercent { get; set; }
    public long MemoryUsageMB { get; set; }

    // Navigation properties
    public ICollection<PlayerSession> PlayerSessions { get; set; } = new List<PlayerSession>();
    public ICollection<TelemetrySnapshot> TelemetrySnapshots { get; set; } = new List<TelemetrySnapshot>();
    public ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
}
