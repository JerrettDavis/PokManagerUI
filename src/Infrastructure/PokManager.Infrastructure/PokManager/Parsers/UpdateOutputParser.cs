using System.Text.RegularExpressions;
using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.Parsers;

/// <summary>
/// Parses POK Manager update command output.
/// </summary>
public class UpdateOutputParser
{
    private static readonly Regex CurrentVersionPattern = new(
        @"Current\s+version:\s*(?<version>[\d\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex LatestVersionPattern = new(
        @"Latest\s+version:\s*(?<version>[\d\.]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex UpdateAvailablePattern = new(
        @"Update\s+available:\s*(?<available>Yes|No|True|False)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex EstimatedSizePattern = new(
        @"Estimated\s+size:\s*(?<size>\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex RequiresRestartPattern = new(
        @"Requires\s+restart:\s*(?<restart>Yes|No|True|False)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex ReleaseNotesPattern = new(
        @"Release\s+notes:\s*(?<notes>.+?)(?=\n[A-Z]|\z)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );

    /// <summary>
    /// Parses check for updates output.
    /// </summary>
    public Result<UpdateAvailability> ParseCheckForUpdates(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<UpdateAvailability>("Output is null or empty");
        }

        var currentVersionMatch = CurrentVersionPattern.Match(output);
        var latestVersionMatch = LatestVersionPattern.Match(output);
        var updateAvailableMatch = UpdateAvailablePattern.Match(output);
        var estimatedSizeMatch = EstimatedSizePattern.Match(output);
        var requiresRestartMatch = RequiresRestartPattern.Match(output);
        var releaseNotesMatch = ReleaseNotesPattern.Match(output);

        var currentVersion = currentVersionMatch.Success
            ? currentVersionMatch.Groups["version"].Value.Trim()
            : "Unknown";

        var latestVersion = latestVersionMatch.Success
            ? latestVersionMatch.Groups["version"].Value.Trim()
            : null;

        var isUpdateAvailable = updateAvailableMatch.Success &&
            (updateAvailableMatch.Groups["available"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             updateAvailableMatch.Groups["available"].Value.Equals("True", StringComparison.OrdinalIgnoreCase));

        long? estimatedSize = null;
        if (estimatedSizeMatch.Success && long.TryParse(estimatedSizeMatch.Groups["size"].Value, out var size))
        {
            estimatedSize = size;
        }

        var requiresRestart = requiresRestartMatch.Success &&
            (requiresRestartMatch.Groups["restart"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             requiresRestartMatch.Groups["restart"].Value.Equals("True", StringComparison.OrdinalIgnoreCase));

        var releaseNotes = releaseNotesMatch.Success
            ? releaseNotesMatch.Groups["notes"].Value.Trim()
            : null;

        var updateAvailability = new UpdateAvailability(
            IsUpdateAvailable: isUpdateAvailable,
            CurrentVersion: currentVersion,
            LatestVersion: latestVersion,
            ReleaseNotes: releaseNotes,
            EstimatedDownloadSizeBytes: estimatedSize,
            RequiresRestart: requiresRestart,
            CheckedAt: DateTimeOffset.UtcNow
        );

        return Result<UpdateAvailability>.Success(updateAvailability);
    }

    /// <summary>
    /// Parses apply updates output.
    /// </summary>
    public Result<UpdateResult> ParseApplyUpdates(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<UpdateResult>("Output is null or empty");
        }

        var previousVersionPattern = new Regex(@"Previous\s+version:\s*(?<version>[\d\.]+)", RegexOptions.IgnoreCase);
        var newVersionPattern = new Regex(@"New\s+version:\s*(?<version>[\d\.]+)", RegexOptions.IgnoreCase);
        var durationPattern = new Regex(@"Duration:\s*(?<duration>[\d:]+)", RegexOptions.IgnoreCase);
        var successPattern = new Regex(@"(Update\s+completed\s+successfully|Success)", RegexOptions.IgnoreCase);
        var restartedPattern = new Regex(@"(Server\s+was\s+restarted:\s*(?<restarted>Yes|No)|Restarting\s+server)", RegexOptions.IgnoreCase);
        var messagePattern = new Regex(@"Message:\s*(?<message>.+)", RegexOptions.IgnoreCase);

        var previousVersionMatch = previousVersionPattern.Match(output);
        var newVersionMatch = newVersionPattern.Match(output);
        var durationMatch = durationPattern.Match(output);
        var successMatch = successPattern.Match(output);
        var restartedMatch = restartedPattern.Match(output);
        var messageMatch = messagePattern.Match(output);

        var previousVersion = previousVersionMatch.Success
            ? previousVersionMatch.Groups["version"].Value.Trim()
            : "Unknown";

        var newVersion = newVersionMatch.Success
            ? newVersionMatch.Groups["version"].Value.Trim()
            : "Unknown";

        var success = successMatch.Success;

        TimeSpan duration = TimeSpan.Zero;
        if (durationMatch.Success && TimeSpan.TryParse(durationMatch.Groups["duration"].Value, out var parsedDuration))
        {
            duration = parsedDuration;
        }

        var wasRestarted = restartedMatch.Success &&
            (restartedMatch.Groups["restarted"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             output.Contains("Restarting server", StringComparison.OrdinalIgnoreCase));

        var requiresRestartMatch = RequiresRestartPattern.Match(output);
        var requiredRestart = requiresRestartMatch.Success &&
            (requiresRestartMatch.Groups["restart"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             requiresRestartMatch.Groups["restart"].Value.Equals("True", StringComparison.OrdinalIgnoreCase));

        // Also check for alternative format: "Server requires restart" without colon
        if (!requiredRestart && output.Contains("Server requires restart", StringComparison.OrdinalIgnoreCase))
        {
            requiredRestart = true;
        }

        var message = messageMatch.Success
            ? messageMatch.Groups["message"].Value.Trim()
            : null;

        var updateResult = new UpdateResult(
            Success: success,
            PreviousVersion: previousVersion,
            NewVersion: newVersion,
            UpdatedAt: DateTimeOffset.UtcNow,
            Duration: duration,
            Message: message,
            RequiredRestart: requiredRestart,
            WasRestarted: wasRestarted
        );

        return Result<UpdateResult>.Success(updateResult);
    }
}
