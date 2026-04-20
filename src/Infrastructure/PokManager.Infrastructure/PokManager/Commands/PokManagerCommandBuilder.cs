using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using PokManager.Domain.Common;
using PokManager.Domain.ValueObjects;

namespace PokManager.Infrastructure.PokManager.Commands;

/// <summary>
/// Fluent builder for constructing safe, properly escaped bash commands for POK Manager operations.
/// </summary>
public sealed class PokManagerCommandBuilder
{
    private static readonly Regex s_dangerousCharactersPattern = new(
        @"[;&|`$()<>\\]",
        RegexOptions.Compiled
    );

    private static readonly Regex s_pathTraversalPattern = new(
        @"\.\./",
        RegexOptions.Compiled
    );

    private readonly string _scriptPath;
    private string? _command;
    private InstanceId? _instanceId;
    private readonly List<string> _flags = new();
    private readonly Dictionary<string, string> _arguments = new();

    private PokManagerCommandBuilder(string scriptPath)
    {
        _scriptPath = scriptPath;
    }

    /// <summary>
    /// Creates a new command builder with the specified script path.
    /// </summary>
    public static PokManagerCommandBuilder Create(string scriptPath)
    {
        return new PokManagerCommandBuilder(scriptPath);
    }

    /// <summary>
    /// Sets the POK Manager command (e.g., "status", "start", "stop").
    /// </summary>
    public PokManagerCommandBuilder WithCommand(string command)
    {
        _command = command;
        return this;
    }

    /// <summary>
    /// Sets the instance ID for the command.
    /// </summary>
    public PokManagerCommandBuilder WithInstanceId(InstanceId instanceId)
    {
        _instanceId = instanceId;
        return this;
    }

    /// <summary>
    /// Adds a flag argument (e.g., --force, --no-confirm).
    /// </summary>
    public PokManagerCommandBuilder WithFlag(string flag)
    {
        _flags.Add(flag);
        return this;
    }

    /// <summary>
    /// Adds a key-value argument (e.g., --map=TheIsland, --players=20).
    /// </summary>
    public PokManagerCommandBuilder WithArgument(string key, string value)
    {
        _arguments[key] = value;
        return this;
    }

    /// <summary>
    /// Builds the final command string with proper escaping and validation.
    /// </summary>
    public Result<string> Build()
    {
        // Validate script path
        if (string.IsNullOrWhiteSpace(_scriptPath))
        {
            return Result.Failure<string>("Script path cannot be empty or whitespace.");
        }

        // Validate command
        if (string.IsNullOrWhiteSpace(_command))
        {
            return Result.Failure<string>("Command is required.");
        }

        // Validate all argument values for security
        foreach (var (key, value) in _arguments)
        {
            var validationResult = ValidateArgumentValue(key, value);
            if (validationResult.IsFailure)
            {
                return Result.Failure<string>(validationResult.Error);
            }
        }

        // Build command
        var commandBuilder = new StringBuilder();

        // Add script path (with escaping if needed)
        commandBuilder.Append(EscapeShellArgument(_scriptPath));
        commandBuilder.Append(' ');

        // Add command
        commandBuilder.Append(_command);

        // Add instance ID if present
        if (_instanceId != null)
        {
            commandBuilder.Append(' ');
            commandBuilder.Append(_instanceId.Value);
        }

        // Add key-value arguments
        foreach (var (key, value) in _arguments)
        {
            commandBuilder.Append(" --");
            commandBuilder.Append(key);
            commandBuilder.Append(' ');
            commandBuilder.Append(EscapeShellArgument(value));
        }

        // Add flags
        foreach (var flag in _flags)
        {
            commandBuilder.Append(" --");
            commandBuilder.Append(flag);
        }

        return Result<string>.Success(commandBuilder.ToString());
    }

    /// <summary>
    /// Validates an argument value for security concerns.
    /// </summary>
    private static Result<Unit> ValidateArgumentValue(string key, string value)
    {
        // Check for dangerous characters that could lead to command injection
        if (s_dangerousCharactersPattern.IsMatch(value))
        {
            return Result.Failure<Unit>(
                $"Argument '{key}' contains dangerous characters that could lead to command injection."
            );
        }

        // Check for path traversal attempts
        if (s_pathTraversalPattern.IsMatch(value))
        {
            return Result.Failure<Unit>(
                $"Argument '{key}' contains path traversal patterns which are not allowed."
            );
        }

        return Result.Success();
    }

    /// <summary>
    /// Escapes a shell argument using single quotes, handling embedded single quotes.
    /// This follows the bash escaping convention: 'value' becomes 'value'\''more'
    /// </summary>
    private static string EscapeShellArgument(string argument)
    {
        // If the argument contains no special characters and no spaces, return as-is
        if (!argument.Contains(' ') &&
            !argument.Contains('\'') &&
            !argument.Contains('"') &&
            !argument.Contains('\\'))
        {
            return argument;
        }

        // Escape single quotes by ending the quoted string, adding an escaped quote,
        // and starting a new quoted string: ' becomes '\''
        var escaped = argument.Replace("'", "'\\''");

        return $"'{escaped}'";
    }
}
