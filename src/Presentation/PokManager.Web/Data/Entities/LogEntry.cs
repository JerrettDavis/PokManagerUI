namespace PokManager.Web.Data.Entities;

/// <summary>
/// Represents a log entry from an instance.
/// </summary>
public class LogEntry
{
    public int Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }

    // Navigation
    public int? InstanceSnapshotId { get; set; }
    public InstanceSnapshot? InstanceSnapshot { get; set; }
}
