using PokManager.Domain.Enumerations;

namespace PokManager.Application.Models;

/// <summary>
/// Options for creating a backup of a Palworld server instance.
/// </summary>
/// <param name="Description">Optional description of the backup.</param>
/// <param name="CompressionFormat">The compression format to use for the backup.</param>
/// <param name="IncludeConfiguration">Whether to include server configuration files in the backup.</param>
/// <param name="IncludeLogs">Whether to include server log files in the backup.</param>
public record CreateBackupOptions(
    string? Description = null,
    CompressionFormat CompressionFormat = CompressionFormat.Gzip,
    bool IncludeConfiguration = true,
    bool IncludeLogs = false
);
