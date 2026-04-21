using System.Globalization;
using System.Text.RegularExpressions;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Parses POK Manager backup list output into ParsedBackupInfo objects.
/// Expected format: backup_&lt;instanceId&gt;_&lt;YYYYMMDD&gt;_&lt;HHMMSS&gt;.tar.[gz|zst]
/// Example: backup_MyServer_20250119_143022.tar.gz
/// </summary>
public class BackupListParser : IPokManagerOutputParser<IReadOnlyList<ParsedBackupInfo>>
{
    private static readonly Regex s_backupPattern = new(
        @"backup_(?<instanceId>.+?)_(?<date>\d{8})_(?<time>\d{6})\.tar\.(?<compression>gz|zst|.+?)(?:\s|$|,)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex s_errorPattern = new(
        @"Error:\s*(?<message>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Result<IReadOnlyList<ParsedBackupInfo>> Parse(string output)
    {
        // Handle null or empty gracefully - return empty list
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<IReadOnlyList<ParsedBackupInfo>>.Success(Array.Empty<ParsedBackupInfo>());
        }

        // Check for error messages first
        var errorMatch = s_errorPattern.Match(output);
        if (errorMatch.Success)
        {
            var errorMessage = errorMatch.Groups["message"].Value.Trim();
            return Result<IReadOnlyList<ParsedBackupInfo>>.Failure($"POK Manager error: {errorMessage}");
        }

        var backups = new List<ParsedBackupInfo>();
        var lines = output.Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            // Extract filename from path if present
            var fileName = Path.GetFileName(trimmedLine);

            // Add space at the end to help regex match
            var match = s_backupPattern.Match(fileName + " ");
            if (!match.Success)
            {
                continue;
            }

            var instanceId = match.Groups["instanceId"].Value;
            var dateStr = match.Groups["date"].Value;
            var timeStr = match.Groups["time"].Value;
            var compressionStr = match.Groups["compression"].Value.ToLowerInvariant();

            // Parse timestamp
            var dateTimeStr = $"{dateStr}{timeStr}";
            if (!DateTimeOffset.TryParseExact(
                dateTimeStr,
                "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var timestamp))
            {
                // Skip invalid timestamps
                continue;
            }

            // Determine compression format
            var compressionFormat = compressionStr switch
            {
                "gz" => CompressionFormat.Gzip,
                "zst" => CompressionFormat.Zstd,
                _ => CompressionFormat.Unknown
            };

            var backupInfo = new ParsedBackupInfo(
                FileName: fileName,
                InstanceId: instanceId,
                Timestamp: timestamp,
                CompressionFormat: compressionFormat
            );

            backups.Add(backupInfo);
        }

        return Result<IReadOnlyList<ParsedBackupInfo>>.Success(backups);
    }
}
