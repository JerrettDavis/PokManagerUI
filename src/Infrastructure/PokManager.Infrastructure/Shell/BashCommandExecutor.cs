using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace PokManager.Infrastructure.Shell;

/// <summary>
/// Executes bash/shell commands using System.Diagnostics.Process.
/// Handles platform detection for Windows/Linux/Mac.
/// </summary>
public sealed class BashCommandExecutor(ILogger<BashCommandExecutor> logger) : IBashCommandExecutor
{
    private readonly ILogger<BashCommandExecutor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<BashCommandResult> ExecuteAsync(
        string command,
        string? workingDirectory,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
        }

        _logger.LogDebug(
            "Executing command: {Command} (WorkingDirectory: {WorkingDirectory}, Timeout: {Timeout}s)",
            command,
            workingDirectory ?? "<current>",
            timeout.TotalSeconds);

        var (fileName, arguments) = GetShellExecutable(command);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory()
            }
        };

        var stdOutBuilder = new System.Text.StringBuilder();
        var stdErrBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                stdOutBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
            {
                stdErrBuilder.AppendLine(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for process completion with timeout and cancellation support
            var completedTask = WaitForExitAsync(process, cancellationToken);

            // Use a separate CTS for timeout to distinguish between timeout and cancellation
            using var timeoutCts = new CancellationTokenSource();
            var timeoutTask = Task.Delay(timeout, timeoutCts.Token);

            var completedFirst = await Task.WhenAny(completedTask, timeoutTask);

            if (completedFirst == timeoutTask)
            {
                // Check if this was due to cancellation or actual timeout
                cancellationToken.ThrowIfCancellationRequested();

                // Timeout occurred
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to kill process after timeout");
                }

                throw new TimeoutException($"Command execution exceeded timeout of {timeout.TotalSeconds} seconds.");
            }

            // Cancel the timeout task since process completed
            timeoutCts.Cancel();

            // Wait for the completion to finish
            await completedTask;

            // Ensure all output is captured
            await Task.Delay(100, cancellationToken); // Small delay to ensure all data is received

            var exitCode = process.ExitCode;
            var stdOut = stdOutBuilder.ToString().TrimEnd();
            var stdErr = stdErrBuilder.ToString().TrimEnd();

            _logger.LogDebug(
                "Command completed with exit code {ExitCode}. StdOut length: {StdOutLength}, StdErr length: {StdErrLength}",
                exitCode,
                stdOut.Length,
                stdErr.Length);

            return new BashCommandResult(exitCode, stdOut, stdErr);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Command execution was cancelled");
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill process after cancellation");
            }

            throw;
        }
        catch (Exception ex) when (ex is not TimeoutException)
        {
            _logger.LogError(ex, "Error executing command: {Command}", command);
            throw;
        }
    }

    private static (string FileName, string Arguments) GetShellExecutable(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // On Windows, use cmd.exe
            return ("cmd.exe", $"/c {command}");
        }
        else
        {
            // On Linux/Mac, use /bin/bash
            return ("/bin/bash", $"-c \"{command.Replace("\"", "\\\"")}\"");
        }
    }

    private static Task WaitForExitAsync(Process process, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<bool>();

        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) => tcs.TrySetResult(true);

        if (process.HasExited)
        {
            tcs.TrySetResult(true);
        }

        cancellationToken.Register(() =>
        {
            tcs.TrySetCanceled(cancellationToken);
        });

        return tcs.Task;
    }
}
