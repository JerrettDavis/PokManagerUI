using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using PokManager.Infrastructure.Shell;
using static TinyBDD.TestContext;
using Xunit;

namespace PokManager.Infrastructure.Tests.Shell;

/// <summary>
/// Tests for the BashCommandExecutor following TDD principles.
/// These tests define the expected behavior before implementation.
/// </summary>
public class BashCommandExecutorTests
{
    private readonly BashCommandExecutor _executor = new(NullLogger<BashCommandExecutor>.Instance);

    [Fact]
    public void ExecuteAsync_ShouldExecuteSuccessfulCommand()
    {
        TestContext.Run
            .Given("a simple echo command", () =>
            {
                // Platform-independent echo command
                var command = OperatingSystem.IsWindows()
                    ? "echo Hello World"
                    : "echo 'Hello World'";

                return command;
            })
            .When("the command is executed", async (command) =>
            {
                var result = await _executor.ExecuteAsync(
                    command,
                    workingDirectory: null,
                    timeout: TimeSpan.FromSeconds(5),
                    cancellationToken: CancellationToken.None);

                return result;
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("the exit code should be 0", (result) =>
            {
                result.ExitCode.Should().Be(0);
            })
            .And("stdout should contain the expected text", (result) =>
            {
                result.StdOut.Should().Contain("Hello World");
            })
            .And("stderr should be empty", (result) =>
            {
                result.StdErr.Should().BeEmpty();
            })
            .Run();
    }

    [Fact]
    public void ExecuteAsync_ShouldCaptureNonZeroExitCode()
    {
        TestContext.Run
            .Given("a command that exits with non-zero code", () =>
            {
                var command = OperatingSystem.IsWindows()
                    ? "cmd /c exit 42"
                    : "exit 42";

                return command;
            })
            .When("the command is executed", async (command) =>
            {
                var result = await _executor.ExecuteAsync(
                    command,
                    workingDirectory: null,
                    timeout: TimeSpan.FromSeconds(5),
                    cancellationToken: CancellationToken.None);

                return result;
            })
            .Then("the result should not be successful", (result) =>
            {
                result.IsSuccess.Should().BeFalse();
            })
            .And("the exit code should be 42", (result) =>
            {
                result.ExitCode.Should().Be(42);
            })
            .Run();
    }

    [Fact]
    public void ExecuteAsync_ShouldCaptureStderr()
    {
        TestContext.Run
            .Given("a command that writes to stderr", () =>
            {
                var command = OperatingSystem.IsWindows()
                    ? "powershell -Command \"Write-Error 'Test Error' 2>&1\""
                    : "echo 'Test Error' >&2";

                return command;
            })
            .When("the command is executed", async (command) =>
            {
                var result = await _executor.ExecuteAsync(
                    command,
                    workingDirectory: null,
                    timeout: TimeSpan.FromSeconds(5),
                    cancellationToken: CancellationToken.None);

                return result;
            })
            .Then("stderr should contain the error message", (result) =>
            {
                result.StdErr.Should().Contain("Test Error");
            })
            .Run();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectTimeout()
    {
        // Given a command that takes longer than the timeout
        var command = OperatingSystem.IsWindows()
            ? "powershell -Command \"Start-Sleep -Seconds 10\""
            : "sleep 10";

        // When the command is executed with a short timeout
        var act = async () => await _executor.ExecuteAsync(
            command,
            workingDirectory: null,
            timeout: TimeSpan.FromMilliseconds(500),
            cancellationToken: CancellationToken.None);

        // Then a TimeoutException should be thrown
        await act.Should().ThrowAsync<TimeoutException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectCancellation()
    {
        // Given a command that takes a long time
        var command = OperatingSystem.IsWindows()
            ? "powershell -Command \"Start-Sleep -Seconds 30\""
            : "sleep 30";

        using var cts = new CancellationTokenSource();

        // When the command is executed and then cancelled
        var task = _executor.ExecuteAsync(
            command,
            workingDirectory: null,
            timeout: TimeSpan.FromSeconds(60),
            cancellationToken: cts.Token);

        // Cancel after a short delay
        await Task.Delay(200);
        cts.Cancel();

        var act = async () => await task;

        // Then an OperationCanceledException should be thrown
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ExecuteAsync_ShouldRespectWorkingDirectory()
    {
        TestContext.Run
            .Given("a command to print the current directory", () =>
            {
                var tempDir = Path.GetTempPath();
                var command = OperatingSystem.IsWindows()
                    ? "cd"
                    : "pwd";

                return (command, tempDir);
            })
            .When("the command is executed with a working directory", async (context) =>
            {
                var (command, tempDir) = context;
                var result = await _executor.ExecuteAsync(
                    command,
                    workingDirectory: tempDir,
                    timeout: TimeSpan.FromSeconds(5),
                    cancellationToken: CancellationToken.None);

                return (result, tempDir);
            })
            .Then("the output should contain the working directory path", (context) =>
            {
                var (result, tempDir) = context;
                result.IsSuccess.Should().BeTrue();
                result.StdOut.Should().Contain(tempDir.TrimEnd(Path.DirectorySeparatorChar));
            })
            .Run();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowWhenCommandIsNull()
    {
        // Given a null command
        string command = null!;

        // When the command is executed
        var act = async () => await _executor.ExecuteAsync(
            command,
            workingDirectory: null,
            timeout: TimeSpan.FromSeconds(5),
            cancellationToken: CancellationToken.None);

        // Then an ArgumentNullException should be thrown
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowWhenTimeoutIsInvalid()
    {
        // Given a command with invalid timeout
        var command = "echo test";

        // When the command is executed with zero or negative timeout
        var act = async () => await _executor.ExecuteAsync(
            command,
            workingDirectory: null,
            timeout: TimeSpan.Zero,
            cancellationToken: CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
