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
/// Tests for PokManagerClient update methods.
/// </summary>
public class PokManagerClientUpdateTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientUpdateTests()
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

    #region CheckForUpdatesAsync Tests

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReturnUpdatesAvailable_WhenNewerVersionExists()
    {
        // Given an instance with available updates
        var checkOutput = @"Checking for updates...
Current version: 0.1.5.1
Latest version: 0.1.6.0
Update available: Yes
Estimated size: 1500000000
Requires restart: Yes
Release notes: Bug fixes and performance improvements";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, checkOutput, ""));

        // When checking for updates
        var result = await _client.CheckForUpdatesAsync("server1", CancellationToken.None);

        // Then the result should indicate updates are available
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUpdateAvailable.Should().BeTrue();
        result.Value.CurrentVersion.Should().Be("0.1.5.1");
        result.Value.LatestVersion.Should().Be("0.1.6.0");
        result.Value.RequiresRestart.Should().BeTrue();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReturnNoUpdates_WhenAlreadyLatest()
    {
        // Given an instance already on the latest version
        var checkOutput = @"Checking for updates...
Current version: 0.1.6.0
Latest version: 0.1.6.0
Update available: No
You are running the latest version";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, checkOutput, ""));

        // When checking for updates
        var result = await _client.CheckForUpdatesAsync("server1", CancellationToken.None);

        // Then the result should indicate no updates available
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUpdateAvailable.Should().BeFalse();
        result.Value.CurrentVersion.Should().Be("0.1.6.0");
        result.Value.LatestVersion.Should().Be("0.1.6.0");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReturnFailure_WhenInstanceNotFound()
    {
        // Given an instance that doesn't exist
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When checking for updates
        var result = await _client.CheckForUpdatesAsync("nonexistent", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to check for updates");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When checking for updates
        var act = async () => await _client.CheckForUpdatesAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldHandleTimeout()
    {
        // Given a command that times out
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Command timed out"));

        // When checking for updates
        var result = await _client.CheckForUpdatesAsync("server1", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    #endregion

    #region ApplyUpdatesAsync Tests

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldApplyUpdates_WhenAvailable()
    {
        // Given an instance with available updates
        var updateOutput = @"Applying update...
Previous version: 0.1.5.1
Downloading update... Done
Installing update... Done
New version: 0.1.6.0
Update completed successfully
Duration: 00:05:30
Server requires restart
Server was restarted: No";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, updateOutput, ""));

        // When applying updates
        var result = await _client.ApplyUpdatesAsync("server1", null, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.PreviousVersion.Should().Be("0.1.5.1");
        result.Value.NewVersion.Should().Be("0.1.6.0");
        result.Value.RequiredRestart.Should().BeTrue();
        result.Value.WasRestarted.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldApplyWithRestart_WhenOptionSpecified()
    {
        // Given update options with restart
        var options = new ApplyUpdatesOptions(
            BackupBeforeUpdate: true,
            StopInstance: true,
            StartAfterUpdate: true,
            ValidateAfterUpdate: true,
            SkipIfNoUpdates: false
        );

        var updateOutput = @"Applying update...
Validation passed
Creating backup... Done
Previous version: 0.1.5.1
Installing update... Done
New version: 0.1.6.0
Restarting server... Done
Update completed successfully";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, updateOutput, ""));

        // When applying updates with options
        var result = await _client.ApplyUpdatesAsync("server1", options, CancellationToken.None);

        // Then the result should be successful with restart
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.WasRestarted.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldApplySpecificVersion_WhenTargetVersionSpecified()
    {
        // Given update options with target version
        var options = new ApplyUpdatesOptions(
            BackupBeforeUpdate: true,
            StopInstance: false,
            StartAfterUpdate: false,
            ValidateAfterUpdate: false,
            SkipIfNoUpdates: false
        );

        var updateOutput = @"Applying update to version 0.1.5.5...
Previous version: 0.1.5.1
Installing update... Done
New version: 0.1.5.5
Update completed successfully";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, updateOutput, ""));

        // When applying updates to specific version
        var result = await _client.ApplyUpdatesAsync("server1", options, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.NewVersion.Should().Be("0.1.5.5");
    }

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldReturnFailure_WhenUpdateFails()
    {
        // Given a command that fails
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Update failed: Download error"));

        // When applying updates
        var result = await _client.ApplyUpdatesAsync("server1", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to apply updates");
    }

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When applying updates
        var act = async () => await _client.ApplyUpdatesAsync(instanceId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyUpdatesAsync_ShouldHandleCancellation()
    {
        // Given a cancelled operation
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // When applying updates
        var result = await _client.ApplyUpdatesAsync("server1", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancel");
    }

    #endregion
}
