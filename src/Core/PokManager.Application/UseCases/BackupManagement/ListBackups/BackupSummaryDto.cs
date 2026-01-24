using PokManager.Domain.Enumerations;

namespace PokManager.Application.UseCases.BackupManagement.ListBackups;

/// <summary>
/// Summary information about a backup.
/// </summary>
/// <param name="BackupId">The unique identifier of the backup.</param>
/// <param name="InstanceId">The instance this backup belongs to.</param>
/// <param name="CreatedAt">When the backup was created.</param>
/// <param name="CompressionFormat">The compression format used for the backup.</param>
/// <param name="FileSizeBytes">The size of the backup file in bytes (only included if IncludeMetadata is true).</param>
public record BackupSummaryDto(
    string BackupId,
    string InstanceId,
    DateTimeOffset CreatedAt,
    CompressionFormat CompressionFormat,
    long? FileSizeBytes);
