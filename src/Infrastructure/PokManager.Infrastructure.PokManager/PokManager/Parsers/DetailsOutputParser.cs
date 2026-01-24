using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.PokManager.Parsers;

/// <summary>
/// Parses POK Manager details command output into a dictionary of key-value pairs.
/// Expected format: Multi-line key-value pairs with colon separator.
/// Example:
/// SessionName: MyServer
/// ServerPassword: secret123
/// MaxPlayers: 20
/// CustomSetting: Value
/// </summary>
public class DetailsOutputParser : IPokManagerOutputParser<Dictionary<string, string>>
{
    private static readonly Regex ErrorPattern = new(
        @"Error:\s*(?<message>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Result<Dictionary<string, string>> Parse(string output)
    {
        if (output == null)
        {
            return Result<Dictionary<string, string>>.Failure("Output is null or empty");
        }

        if (output == string.Empty)
        {
            return Result<Dictionary<string, string>>.Failure("Output is null or empty");
        }

        // Check for error messages first
        var errorMatch = ErrorPattern.Match(output);
        if (errorMatch.Success)
        {
            var errorMessage = errorMatch.Groups["message"].Value.Trim();
            return Result<Dictionary<string, string>>.Failure($"POK Manager error: {errorMessage}");
        }

        var settings = new Dictionary<string, string>();

        // Handle whitespace-only strings (not empty, just whitespace like "\n\n\n")
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result<Dictionary<string, string>>.Success(settings);
        }

        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                continue;
            }

            // Find the first colon to split key and value
            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex <= 0)
            {
                // Skip lines without a colon
                continue;
            }

            var key = trimmedLine.Substring(0, colonIndex).Trim();
            var value = trimmedLine.Substring(colonIndex + 1).Trim();

            // Store the key-value pair (overwrite if duplicate)
            settings[key] = value;
        }

        return Result<Dictionary<string, string>>.Success(settings);
    }
}
