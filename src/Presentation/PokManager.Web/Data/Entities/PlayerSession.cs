namespace PokManager.Web.Data.Entities;

/// <summary>
/// Represents a player's gaming session on an instance.
/// </summary>
public class PlayerSession
{
    public int Id { get; set; }
    public string InstanceId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public DateTime? LeftAt { get; set; }
    public int Level { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    
    // Navigation
    public int? InstanceSnapshotId { get; set; }
    public InstanceSnapshot? InstanceSnapshot { get; set; }
}
