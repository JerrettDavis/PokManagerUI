using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Represents parsed backup information from a backup filename.
/// </summary>
/// <param name="FileName">The original backup filename.</param>
/// <param name="InstanceId">The instance ID this backup belongs to.</param>
/// <param name="Timestamp">The timestamp when the backup was created.</param>
/// <param name="CompressionFormat">The compression format used.</param>
public record ParsedBackupInfo(
    string FileName,
    string InstanceId,
    DateTimeOffset Timestamp,
    CompressionFormat CompressionFormat
);
