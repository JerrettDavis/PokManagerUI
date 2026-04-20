using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Parses POK Manager error output into categorized PokManagerError objects.
/// Expected format: "Error: <message>"
/// Categorizes errors based on message content keywords.
/// </summary>
public class ErrorOutputParser : IPokManagerOutputParser<PokManagerError>
{
    private static readonly Regex s_errorPattern = new(
        @"Error:\s*(?<message>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline
    );

    private static readonly Dictionary<PokManagerErrorCode, string[]> s_errorPatterns = new()
    {
        [PokManagerErrorCode.InstanceNotFound] = new[] { "instance not found", "does not exist", "cannot find", "unknown instance" },
        [PokManagerErrorCode.InstanceAlreadyExists] = new[] { "already exists", "duplicate" },
        [PokManagerErrorCode.InstanceNotRunning] = new[] { "not running", "is stopped", "not started" },
        [PokManagerErrorCode.InstanceAlreadyRunning] = new[] { "already running", "is running" },
        [PokManagerErrorCode.PermissionDenied] = new[] { "permission denied", "access denied", "unauthorized", "forbidden" },
        [PokManagerErrorCode.InvalidConfiguration] = new[] { "invalid configuration", "configuration error", "invalid setting" },
        [PokManagerErrorCode.InvalidState] = new[] { "invalid state", "current state", "cannot perform" },
        [PokManagerErrorCode.BackupNotFound] = new[] { "backup file not found", "backup not found" },
        [PokManagerErrorCode.InsufficientDiskSpace] = new[] { "disk space", "no space", "storage full" },
        [PokManagerErrorCode.NetworkError] = new[] { "network", "connection failed", "cannot connect" },
        [PokManagerErrorCode.TimeoutError] = new[] { "timeout", "timed out", "time out" }
    };

    public Result<PokManagerError> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<PokManagerError>.Failure("Output is null or empty");
        }

        // Check if this is an error message
        var match = s_errorPattern.Match(output);
        if (!match.Success)
        {
            return Result<PokManagerError>.Failure("Output is not an error message");
        }

        var errorMessage = match.Groups["message"].Value.Trim();
        var errorCode = CategorizeError(errorMessage);

        var error = new PokManagerError(
            ErrorCode: errorCode,
            Message: errorMessage,
            RawOutput: output
        );

        return Result<PokManagerError>.Success(error);
    }

    private static PokManagerErrorCode CategorizeError(string message)
    {
        var lowerMessage = message.ToLowerInvariant();

        // Check patterns in priority order
        foreach (var (errorCode, patterns) in s_errorPatterns)
        {
            foreach (var pattern in patterns)
            {
                if (lowerMessage.Contains(pattern))
                {
                    return errorCode;
                }
            }
        }

        // Fallback: check for generic "not found" which likely means instance not found
        if (lowerMessage.Contains("not found"))
        {
            return PokManagerErrorCode.InstanceNotFound;
        }

        return PokManagerErrorCode.Unknown;
    }
}
