namespace PokManager.Application.UseCases.BackupManagement.UploadBackup;

/// <summary>
/// Request to upload a backup file from an external source.
/// </summary>
/// <param name="InstanceId">The instance ID to associate the backup with.</param>
/// <param name="FileName">The original filename of the uploaded backup.</param>
/// <param name="FileStream">The stream containing the backup file data.</param>
/// <param name="Description">Optional description for the backup.</param>
/// <param name="CorrelationId">Correlation ID for tracking.</param>
/// <param name="RestoreImmediately">Whether to restore this backup immediately after upload.</param>
public record UploadBackupRequest(
    string InstanceId,
    string FileName,
    Stream FileStream,
    string? Description,
    string CorrelationId,
    bool RestoreImmediately = false
);
