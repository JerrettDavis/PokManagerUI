namespace PokManager.Application.Models;

/// <summary>
/// Options for restoring a backup to a Palworld server instance.
/// </summary>
/// <param name="StopInstance">Whether to stop the instance before restoring.</param>
/// <param name="StartAfterRestore">Whether to start the instance after restore completes.</param>
/// <param name="BackupBeforeRestore">Whether to create a backup before restoring.</param>
/// <param name="ValidateBackup">Whether to validate the backup file before restoring.</param>
public record RestoreBackupOptions(
    bool StopInstance = true,
    bool StartAfterRestore = false,
    bool BackupBeforeRestore = true,
    bool ValidateBackup = true
);
