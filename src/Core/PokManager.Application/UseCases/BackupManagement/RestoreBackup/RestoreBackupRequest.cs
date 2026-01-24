namespace PokManager.Application.UseCases.BackupManagement.RestoreBackup;

public record RestoreBackupRequest(
    string InstanceId,
    string BackupId,
    string CorrelationId,
    bool Confirmed = false,
    bool CreateSafetyBackup = true);
