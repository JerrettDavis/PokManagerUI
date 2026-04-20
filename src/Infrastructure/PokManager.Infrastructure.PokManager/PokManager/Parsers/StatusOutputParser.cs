using System.Text.RegularExpressions;
using PokManager.Application.Models;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Parses POK Manager status command output into InstanceStatus DTOs.
/// Expected format: "Instance: <id>, State: <state>, Container: <id>, Health: <health>"
/// </summary>
public class StatusOutputParser : IPokManagerOutputParser<InstanceStatus>
{
    private static readonly Regex s_statusPattern = new(
        @"Instance:\s*(?<instance>[^,]+)\s*,\s*State:\s*(?<state>[^,]+)\s*,\s*Container:\s*(?<container>[^,]+)\s*,\s*Health:\s*(?<health>[^,\s]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex s_errorPattern = new(
        @"Error:\s*(?<message>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Result<InstanceStatus> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<InstanceStatus>.Failure("Output is null or empty");
        }

        // Check for error messages first
        var errorMatch = s_errorPattern.Match(output);
        if (errorMatch.Success)
        {
            var errorMessage = errorMatch.Groups["message"].Value.Trim();
            return Result<InstanceStatus>.Failure($"POK Manager error: {errorMessage}");
        }

        // Parse status output
        var match = s_statusPattern.Match(output);
        if (!match.Success)
        {
            return Result<InstanceStatus>.Failure("Failed to parse status output: invalid format");
        }

        var instanceId = match.Groups["instance"].Value.Trim();
        var stateStr = match.Groups["state"].Value.Trim();
        var healthStr = match.Groups["health"].Value.Trim();

        // Parse state enum
        if (!Enum.TryParse<InstanceState>(stateStr, ignoreCase: true, out var state))
        {
            return Result<InstanceStatus>.Failure($"Invalid State value: {stateStr}");
        }

        // Parse health enum
        if (!Enum.TryParse<ProcessHealth>(healthStr, ignoreCase: true, out var health))
        {
            return Result<InstanceStatus>.Failure($"Invalid Health value: {healthStr}");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return Result<InstanceStatus>.Failure("Missing Instance field");
        }

        if (state == InstanceState.Unknown)
        {
            return Result<InstanceStatus>.Failure("Missing State field");
        }

        // Create InstanceStatus DTO
        var status = new InstanceStatus(
            InstanceId: instanceId,
            State: state,
            Health: health,
            Uptime: null, // Not available in basic status output
            PlayerCount: 0, // Not available in basic status output
            MaxPlayers: 0, // Not available in basic status output
            Version: null, // Not available in basic status output
            LastUpdated: DateTimeOffset.UtcNow
        );

        return Result<InstanceStatus>.Success(status);
    }
}
