using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Parses POK Manager list instances output into a list of instance IDs.
/// Expected formats:
/// - Newline-delimited: "Instance_Server1\nInstance_Server2\n..."
/// - Comma-delimited: "Instance_Server1, Instance_Server2, ..."
/// - Directory listing: Contains "Instance_*" prefixed names
/// </summary>
public class ListInstancesParser : IPokManagerOutputParser<IReadOnlyList<string>>
{
    private static readonly Regex s_instancePattern = new(
        @"\bInstance_(?<instanceId>[^\s,\r\n]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex s_errorPattern = new(
        @"Error:\s*(?<message>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Result<IReadOnlyList<string>> Parse(string output)
    {
        // Handle null or empty gracefully - return empty list
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<IReadOnlyList<string>>.Success(Array.Empty<string>());
        }

        // Check for error messages first
        var errorMatch = s_errorPattern.Match(output);
        if (errorMatch.Success)
        {
            var errorMessage = errorMatch.Groups["message"].Value.Trim();
            return Result<IReadOnlyList<string>>.Failure($"POK Manager error: {errorMessage}");
        }

        // Find all instances using regex
        var instances = new List<string>();
        var matches = s_instancePattern.Matches(output);

        foreach (Match match in matches)
        {
            if (match.Success)
            {
                var instanceId = match.Groups["instanceId"].Value.Trim();
                instances.Add(instanceId);
            }
        }

        return Result<IReadOnlyList<string>>.Success(instances);
    }
}
