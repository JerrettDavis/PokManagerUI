namespace PokManager.Application.UseCases.BackupManagement.ListBackups;

/// <summary>
/// Response containing the list of backups for an instance.
/// </summary>
/// <param name="Backups">The list of backup summaries.</param>
public record ListBackupsResponse(
    IReadOnlyList<BackupSummaryDto> Backups);
