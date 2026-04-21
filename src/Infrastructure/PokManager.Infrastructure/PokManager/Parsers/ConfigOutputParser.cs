using System.Text.RegularExpressions;
using PokManager.Application.Models;
using PokManager.Domain.Common;

namespace PokManager.Infrastructure.PokManager.Parsers;

/// <summary>
/// Parses POK Manager configuration command output.
/// </summary>
public class ConfigOutputParser
{
    private static readonly Regex s_configLinePattern = new(
        @"^(?<key>[^:]+):\s*(?<value>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex s_validationResultPattern = new(
        @"Configuration\s+validation:\s*(?<result>PASSED|FAILED)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex s_errorLinePattern = new(
        @"^\s*-\s*(?<error>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex s_warningLinePattern = new(
        @"^\s*-\s*(?<warning>.+)$",
        RegexOptions.Compiled
    );

    private static readonly Regex s_changedSettingsPattern = new(
        @"Changed\s+settings:\s*(?<settings>.+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    /// <summary>
    /// Parses configuration get output into a dictionary.
    /// </summary>
    public Result<IReadOnlyDictionary<string, string>> ParseConfiguration(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<IReadOnlyDictionary<string, string>>("Output is null or empty");
        }

        var config = new Dictionary<string, string>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            var match = s_configLinePattern.Match(trimmedLine);

            if (match.Success)
            {
                var key = match.Groups["key"].Value.Trim();
                var value = match.Groups["value"].Value.Trim();
                config[key] = value;
            }
        }

        if (config.Count == 0)
        {
            return Result.Failure<IReadOnlyDictionary<string, string>>("No configuration found in output");
        }

        return Result<IReadOnlyDictionary<string, string>>.Success(config);
    }

    /// <summary>
    /// Parses configuration validation output.
    /// </summary>
    public Result<ConfigurationValidationResult> ParseValidation(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<ConfigurationValidationResult>("Output is null or empty");
        }

        var validationMatch = s_validationResultPattern.Match(output);
        var isValid = validationMatch.Success &&
            validationMatch.Groups["result"].Value.Equals("PASSED", StringComparison.OrdinalIgnoreCase);

        var errors = new List<string>();
        var warnings = new List<string>();

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var inErrorsSection = false;
        var inWarningsSection = false;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (trimmedLine.Contains("errors:", StringComparison.OrdinalIgnoreCase))
            {
                inErrorsSection = true;
                inWarningsSection = false;

                // Check if count is specified on same line
                var errorCountMatch = Regex.Match(trimmedLine, @"(\d+)\s+errors?:", RegexOptions.IgnoreCase);
                if (errorCountMatch.Success && errorCountMatch.Groups[1].Value == "0")
                {
                    inErrorsSection = false;
                }
                continue;
            }

            if (trimmedLine.Contains("warnings:", StringComparison.OrdinalIgnoreCase))
            {
                inWarningsSection = true;
                inErrorsSection = false;

                // Check if count is specified on same line
                var warningCountMatch = Regex.Match(trimmedLine, @"(\d+)\s+warnings?:", RegexOptions.IgnoreCase);
                if (warningCountMatch.Success && warningCountMatch.Groups[1].Value == "0")
                {
                    inWarningsSection = false;
                }
                continue;
            }

            // Parse error/warning lines
            if (trimmedLine.StartsWith("-") || trimmedLine.StartsWith("*"))
            {
                var content = trimmedLine.TrimStart('-', '*').Trim();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    if (inErrorsSection)
                    {
                        errors.Add(content);
                    }
                    else if (inWarningsSection)
                    {
                        warnings.Add(content);
                    }
                }
            }
        }

        var validationResult = new ConfigurationValidationResult(
            IsValid: isValid,
            Errors: errors,
            Warnings: warnings,
            ValidatedAt: DateTimeOffset.UtcNow
        );

        return Result<ConfigurationValidationResult>.Success(validationResult);
    }

    /// <summary>
    /// Parses configuration apply output.
    /// </summary>
    public Result<ApplyConfigurationResult> ParseApplyConfiguration(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return Result.Failure<ApplyConfigurationResult>("Output is null or empty");
        }

        var successPattern = new Regex(@"Configuration\s+applied\s+successfully", RegexOptions.IgnoreCase);
        var success = successPattern.IsMatch(output);

        var changedSettings = new List<string>();
        var changedSettingsMatch = s_changedSettingsPattern.Match(output);
        if (changedSettingsMatch.Success)
        {
            var settingsStr = changedSettingsMatch.Groups["settings"].Value;
            changedSettings.AddRange(
                settingsStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
            );
        }

        var requiresRestartPattern = new Regex(@"Restart\s+required:\s*(?<restart>Yes|No|True|False)", RegexOptions.IgnoreCase);
        var requiresRestartMatch = requiresRestartPattern.Match(output);
        var requiredRestart = requiresRestartMatch.Success &&
            (requiresRestartMatch.Groups["restart"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             requiresRestartMatch.Groups["restart"].Value.Equals("True", StringComparison.OrdinalIgnoreCase));

        var wasRestartedPattern = new Regex(@"Server\s+was\s+restarted:\s*(?<restarted>Yes|No|True|False)", RegexOptions.IgnoreCase);
        var wasRestartedMatch = wasRestartedPattern.Match(output);
        var wasRestarted = wasRestartedMatch.Success &&
            (wasRestartedMatch.Groups["restarted"].Value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
             wasRestartedMatch.Groups["restarted"].Value.Equals("True", StringComparison.OrdinalIgnoreCase));

        // Also check for alternative format: "Server restarted" without colon
        if (!wasRestarted && output.Contains("Server restarted", StringComparison.OrdinalIgnoreCase))
        {
            wasRestarted = true;
        }

        var backupCreatedPattern = new Regex(@"Backup\s+created:\s*(?<created>Yes|No|True|False|backup_\w+)", RegexOptions.IgnoreCase);
        var backupCreatedMatch = backupCreatedPattern.Match(output);
        var backupCreated = backupCreatedMatch.Success &&
            !backupCreatedMatch.Groups["created"].Value.Equals("No", StringComparison.OrdinalIgnoreCase) &&
            !backupCreatedMatch.Groups["created"].Value.Equals("False", StringComparison.OrdinalIgnoreCase);

        var messagePattern = new Regex(@"Message:\s*(?<message>.+)", RegexOptions.IgnoreCase);
        var messageMatch = messagePattern.Match(output);
        var message = messageMatch.Success ? messageMatch.Groups["message"].Value.Trim() : null;

        var result = new ApplyConfigurationResult(
            Success: success,
            ChangedSettings: changedSettings,
            RequiredRestart: requiredRestart,
            WasRestarted: wasRestarted,
            BackupCreated: backupCreated,
            AppliedAt: DateTimeOffset.UtcNow,
            Message: message
        );

        return Result<ApplyConfigurationResult>.Success(result);
    }
}
