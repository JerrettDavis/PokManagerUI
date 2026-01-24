namespace PokManager.Infrastructure.Shell;

/// <summary>
/// Represents the result of a bash command execution.
/// </summary>
public sealed class BashCommandResult
{
    /// <summary>
    /// Gets the exit code returned by the command.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>
    /// Gets the standard output captured from the command.
    /// </summary>
    public string StdOut { get; init; } = string.Empty;

    /// <summary>
    /// Gets the standard error output captured from the command.
    /// </summary>
    public string StdErr { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether the command executed successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Creates a new instance of BashCommandResult.
    /// </summary>
    public BashCommandResult(int exitCode, string stdOut, string stdErr)
    {
        ExitCode = exitCode;
        StdOut = stdOut ?? string.Empty;
        StdErr = stdErr ?? string.Empty;
    }
}
