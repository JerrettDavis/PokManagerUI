using PokManager.Domain.Enumerations;

namespace PokManager.Web.Models;

/// <summary>
/// View model for restoring a backup.
/// </summary>
public class RestoreBackupViewModel
{
    public string BackupId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public CompressionFormat CompressionFormat { get; set; }

    // Restore options
    public bool StopInstance { get; set; } = true;
    public bool StartAfterRestore { get; set; } = false;
    public bool BackupBeforeRestore { get; set; } = true;
    public bool ValidateBackup { get; set; } = true;
}
