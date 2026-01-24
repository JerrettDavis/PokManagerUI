namespace PokManager.Application.UseCases.BackupManagement.CreateBackup;

/// <summary>
/// Response for backup creation operations.
/// </summary>
/// <param name="Success">Indicates whether the backup was created successfully.</param>
/// <param name="BackupId">The unique identifier of the created backup.</param>
/// <param name="InstanceId">The instance identifier the backup was created for.</param>
/// <param name="FilePath">The file path where the backup is stored.</param>
/// <param name="SizeInBytes">The size of the backup file in bytes.</param>
/// <param name="CreatedAt">The timestamp when the backup was created.</param>
/// <param name="Duration">The time taken to create the backup.</param>
public record CreateBackupResponse(
    bool Success,
    string BackupId,
    string InstanceId,
    string? FilePath = null,
    long? SizeInBytes = null,
    DateTimeOffset? CreatedAt = null,
    TimeSpan? Duration = null
);
