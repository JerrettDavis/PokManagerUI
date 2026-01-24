using PokManager.Application.Models;

namespace PokManager.Application.UseCases.BackupManagement.CreateBackup;

public record CreateBackupRequest(
    string InstanceId,
    string CorrelationId,
    CreateBackupOptions? Options = null);
