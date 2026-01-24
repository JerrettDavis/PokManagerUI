using PokManager.Domain.Enumerations;

namespace PokManager.Application.Models;

/// <summary>
/// Represents information about a backup of a Palworld server instance.
/// </summary>
/// <param name="BackupId">The unique identifier of the backup.</param>
/// <param name="InstanceId">The instance this backup belongs to.</param>
/// <param name="Description">Optional description of the backup.</param>
/// <param name="CompressionFormat">The compression format used for the backup.</param>
/// <param name="SizeInBytes">The size of the backup file in bytes.</param>
/// <param name="CreatedAt">When the backup was created.</param>
/// <param name="FilePath">The file system path to the backup file.</param>
/// <param name="IsAutomatic">Whether this backup was created automatically.</param>
/// <param name="ServerVersion">The server version at the time of backup.</param>
public record BackupInfo(
    string BackupId,
    string InstanceId,
    string? Description,
    CompressionFormat CompressionFormat,
    long SizeInBytes,
    DateTimeOffset CreatedAt,
    string FilePath,
    bool IsAutomatic,
    string? ServerVersion
);
