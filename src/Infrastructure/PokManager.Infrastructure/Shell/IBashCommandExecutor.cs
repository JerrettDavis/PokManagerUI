namespace PokManager.Infrastructure.Shell;

/// <summary>
/// Interface for executing bash/shell commands asynchronously.
/// </summary>
public interface IBashCommandExecutor
{
    /// <summary>
    /// Executes a shell command asynchronously.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="workingDirectory">The working directory for the command. If null, uses the current directory.</param>
    /// <param name="timeout">Maximum time to wait for command completion.</param>
    /// <param name="cancellationToken">Cancellation token to stop the command execution.</param>
    /// <returns>A BashCommandResult containing the execution results.</returns>
    /// <exception cref="ArgumentNullException">Thrown when command is null.</exception>
    /// <exception cref="ArgumentException">Thrown when timeout is zero or negative.</exception>
    /// <exception cref="TimeoutException">Thrown when command execution exceeds the timeout.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    Task<BashCommandResult> ExecuteAsync(
        string command,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
