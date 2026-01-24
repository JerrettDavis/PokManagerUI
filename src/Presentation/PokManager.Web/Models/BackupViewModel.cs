using PokManager.Domain.Enumerations;

namespace PokManager.Web.Models;

/// <summary>
/// View model for displaying backup information in the UI.
/// </summary>
public class BackupViewModel
{
    public string BackupId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CompressionFormat CompressionFormat { get; set; }
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsAutomatic { get; set; }
    public string? ServerVersion { get; set; }
}
