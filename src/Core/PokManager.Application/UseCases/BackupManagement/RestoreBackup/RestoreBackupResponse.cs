namespace PokManager.Application.UseCases.BackupManagement.RestoreBackup;

/// <summary>
/// Response from a backup restoration operation.
/// </summary>
/// <param name="Success">Whether the restore operation was successful.</param>
/// <param name="BackupId">The ID of the backup that was restored.</param>
/// <param name="InstanceId">The ID of the instance that was restored.</param>
/// <param name="SafetyBackupId">The ID of the safety backup created before restore, if any.</param>
/// <param name="Duration">How long the restore operation took.</param>
/// <param name="Message">Additional information about the restore operation.</param>
public record RestoreBackupResponse(
    bool Success,
    string BackupId,
    string InstanceId,
    string? SafetyBackupId,
    TimeSpan Duration,
    string? Message = null
);
