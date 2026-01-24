namespace PokManager.Application.UseCases.BackupManagement.UploadBackup;

/// <summary>
/// Response from uploading a backup file.
/// </summary>
/// <param name="Success">Whether the upload was successful.</param>
/// <param name="BackupId">The ID assigned to the uploaded backup.</param>
/// <param name="InstanceId">The instance ID the backup was uploaded for.</param>
/// <param name="FilePath">The file path where the backup was stored.</param>
/// <param name="SizeInBytes">The size of the uploaded backup in bytes.</param>
/// <param name="Restored">Whether the backup was also restored.</param>
/// <param name="Message">Additional information about the upload.</param>
public record UploadBackupResponse(
    bool Success,
    string BackupId,
    string InstanceId,
    string FilePath,
    long SizeInBytes,
    bool Restored,
    string? Message = null
);
