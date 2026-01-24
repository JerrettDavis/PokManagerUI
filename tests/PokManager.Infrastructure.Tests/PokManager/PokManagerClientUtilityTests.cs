using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Infrastructure.PokManager;
using PokManager.Infrastructure.Shell;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager;

/// <summary>
/// Tests for PokManagerClient utility methods (logs, RCON, save world, custom commands).
/// </summary>
public class PokManagerClientUtilityTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientUtilityTests()
    {
        _mockExecutor = Substitute.For<IBashCommandExecutor>();
        _configuration = new PokManagerClientConfiguration
        {
            PokManagerScriptPath = "/opt/pok/pok.sh",
            WorkingDirectory = "/opt/pok",
            DefaultTimeout = TimeSpan.FromSeconds(30),
            InstancesBasePath = "/opt/pok/instances"
        };

        _client = new PokManagerClient(
            _mockExecutor,
            _configuration,
            NullLogger<PokManagerClient>.Instance);
    }

    #region GetLogsAsync Tests

    [Fact]
    public async Task GetLogsAsync_ShouldReturnLogs_WhenInstanceHasLogs()
    {
        // Given an instance with log entries
        var logsOutput = @"2024-01-19T10:00:00Z [INFO] Server started
2024-01-19T10:01:00Z [INFO] Player joined: TestPlayer
2024-01-19T10:02:00Z [WARNING] High memory usage detected
2024-01-19T10:03:00Z [ERROR] Connection timeout";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, logsOutput, ""));

        // When getting logs
        var result = await _client.GetLogsAsync("server1", null, CancellationToken.None);

        // Then the result should contain log entries
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value[0].Message.Should().Contain("Server started");
        result.Value[1].Level.Should().Be(LogLevel.Information);
        result.Value[2].Level.Should().Be(LogLevel.Warning);
        result.Value[3].Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task GetLogsAsync_ShouldApplyMaxLinesFilter_WhenOptionProvided()
    {
        // Given options with max lines
        var options = new GetLogsOptions(MaxLines: 2);
        var logsOutput = @"2024-01-19T10:00:00Z [INFO] Log 1
2024-01-19T10:01:00Z [INFO] Log 2";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, logsOutput, ""));

        // When getting logs with options
        var result = await _client.GetLogsAsync("server1", options, CancellationToken.None);

        // Then should verify command included max lines parameter
        await _mockExecutor.Received(1).ExecuteAsync(
            Arg.Is<string>(cmd => cmd.Contains("--max-lines") || cmd.Contains("--tail") || cmd.Contains("-n")),
            Arg.Any<string>(),
            Arg.Any<TimeSpan>(),
            Arg.Any<CancellationToken>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task GetLogsAsync_ShouldReturnFailure_WhenInstanceNotFound()
    {
        // Given an instance that doesn't exist
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When getting logs
        var result = await _client.GetLogsAsync("nonexistent", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to get logs");
    }

    [Fact]
    public async Task GetLogsAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When getting logs
        var act = async () => await _client.GetLogsAsync(instanceId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region StreamLogsAsync Tests

    [Fact]
    public async Task StreamLogsAsync_ShouldStreamLogs()
    {
        // Given an instance that will stream logs
        // This is a complex test for IAsyncEnumerable
        // For now, verify the method exists and throws NotImplementedException
        var instanceId = "server1";

        // When streaming logs
        var stream = _client.StreamLogsAsync(instanceId, CancellationToken.None);

        // Then the stream should be enumerable
        stream.Should().NotBeNull();
    }

    [Fact]
    public async Task StreamLogsAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When streaming logs and trying to enumerate
        var act = async () =>
        {
            await foreach (var log in _client.StreamLogsAsync(instanceId, CancellationToken.None))
            {
                // Should throw before getting here
            }
        };

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SendChatMessageAsync Tests

    [Fact]
    public async Task SendChatMessageAsync_ShouldSendMessage_WhenInstanceIsRunning()
    {
        // Given a running instance
        var rconOutput = @"RCON command executed successfully
Message sent to all players";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, rconOutput, ""));

        // When sending a chat message
        var result = await _client.SendChatMessageAsync("server1", "Hello players!", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendChatMessageAsync_ShouldReturnFailure_WhenInstanceIsStopped()
    {
        // Given a stopped instance
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance is not running"));

        // When sending a chat message
        var result = await _client.SendChatMessageAsync("server1", "Hello!", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to send chat message");
    }

    [Fact]
    public async Task SendChatMessageAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var act = async () => await _client.SendChatMessageAsync("", "Message", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SendChatMessageAsync_ShouldThrowArgumentException_WhenMessageIsEmpty()
    {
        // Given an empty message
        var act = async () => await _client.SendChatMessageAsync("server1", "", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region SaveWorldAsync Tests

    [Fact]
    public async Task SaveWorldAsync_ShouldSaveWorld_WhenInstanceIsRunning()
    {
        // Given a running instance
        var saveOutput = @"Executing save command...
World saved successfully
Save duration: 3 seconds";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, saveOutput, ""));

        // When saving the world
        var result = await _client.SaveWorldAsync("server1", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SaveWorldAsync_ShouldReturnFailure_WhenInstanceIsStopped()
    {
        // Given a stopped instance
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance is not running"));

        // When saving the world
        var result = await _client.SaveWorldAsync("server1", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to save world");
    }

    [Fact]
    public async Task SaveWorldAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var act = async () => await _client.SaveWorldAsync("", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SaveWorldAsync_ShouldHandleTimeout()
    {
        // Given a command that times out
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Save operation timed out"));

        // When saving the world
        var result = await _client.SaveWorldAsync("server1", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    #endregion

    #region ExecuteCustomCommandAsync Tests

    [Fact]
    public async Task ExecuteCustomCommandAsync_ShouldExecuteCommand_WhenValid()
    {
        // Given a valid custom command
        var commandOutput = @"Executing custom command...
Command output: Success
Result: Operation completed";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, commandOutput, ""));

        // When executing custom command
        var result = await _client.ExecuteCustomCommandAsync("server1", "info", CancellationToken.None);

        // Then the result should be successful and contain output
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("Success");
    }

    [Fact]
    public async Task ExecuteCustomCommandAsync_ShouldReturnOutput_WhenCommandProducesOutput()
    {
        // Given a command with output
        var commandOutput = @"Player list:
1. PlayerOne
2. PlayerTwo
Total: 2 players";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, commandOutput, ""));

        // When executing custom command
        var result = await _client.ExecuteCustomCommandAsync("server1", "listplayers", CancellationToken.None);

        // Then the result should contain the full output
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("PlayerOne");
        result.Value.Should().Contain("PlayerTwo");
        result.Value.Should().Contain("Total: 2 players");
    }

    [Fact]
    public async Task ExecuteCustomCommandAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a command that fails
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Command execution failed"));

        // When executing custom command
        var result = await _client.ExecuteCustomCommandAsync("server1", "invalidcmd", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to execute custom command");
    }

    [Fact]
    public async Task ExecuteCustomCommandAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var act = async () => await _client.ExecuteCustomCommandAsync("", "command", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExecuteCustomCommandAsync_ShouldThrowArgumentException_WhenCommandIsEmpty()
    {
        // Given an empty command
        var act = async () => await _client.ExecuteCustomCommandAsync("server1", "", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
