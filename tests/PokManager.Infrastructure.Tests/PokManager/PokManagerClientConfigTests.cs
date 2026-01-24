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
/// Tests for PokManagerClient configuration methods.
/// </summary>
public class PokManagerClientConfigTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientConfigTests()
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

    #region GetConfigurationAsync Tests

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnConfiguration_WhenInstanceExists()
    {
        // Given an instance with configuration
        var detailsOutput = @"ServerName: My Server
Port: 8211
MaxPlayers: 32
Difficulty: Normal
DayTimeSpeedRate: 1.0
NightTimeSpeedRate: 1.0";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, detailsOutput, ""));

        // When getting configuration
        var result = await _client.GetConfigurationAsync("server1", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().ContainKey("ServerName");
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldReturnFailure_WhenInstanceNotFound()
    {
        // Given an instance that doesn't exist
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When getting configuration
        var result = await _client.GetConfigurationAsync("nonexistent", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to get configuration");
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When getting configuration
        var act = async () => await _client.GetConfigurationAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetConfigurationAsync_ShouldHandleTimeout()
    {
        // Given a command that times out
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Command timed out"));

        // When getting configuration
        var result = await _client.GetConfigurationAsync("server1", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    #endregion

    #region ValidateConfigurationAsync Tests

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldReturnValid_WhenConfigurationIsValid()
    {
        // Given valid configuration
        var config = new Dictionary<string, string>
        {
            { "ServerName", "My Server" },
            { "Port", "8211" },
            { "MaxPlayers", "32" }
        };

        var validateOutput = @"Configuration validation: PASSED
No errors found
2 warnings:
- Port 8211 is the default, consider changing it
- MaxPlayers should be between 1 and 32";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, validateOutput, ""));

        // When validating configuration
        var result = await _client.ValidateConfigurationAsync("server1", config, CancellationToken.None);

        // Then the result should be successful and valid
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Errors.Should().BeEmpty();
        result.Value.Warnings.Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldReturnInvalid_WhenConfigurationHasErrors()
    {
        // Given invalid configuration
        var config = new Dictionary<string, string>
        {
            { "ServerName", "" },
            { "Port", "invalid" },
            { "MaxPlayers", "-1" }
        };

        var validateOutput = @"Configuration validation: FAILED
3 errors:
- ServerName cannot be empty
- Port must be a valid number
- MaxPlayers must be positive";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, validateOutput, ""));

        // When validating configuration
        var result = await _client.ValidateConfigurationAsync("server1", config, CancellationToken.None);

        // Then the result should be successful but invalid
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeFalse();
        result.Value.Errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a command that fails
        var config = new Dictionary<string, string> { { "ServerName", "Test" } };

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Validation command failed"));

        // When validating configuration
        var result = await _client.ValidateConfigurationAsync("server1", config, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to validate configuration");
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var config = new Dictionary<string, string> { { "ServerName", "Test" } };

        // When validating configuration
        var act = async () => await _client.ValidateConfigurationAsync("", config, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ValidateConfigurationAsync_ShouldThrowArgumentException_WhenConfigurationIsNull()
    {
        // Given null configuration
        IReadOnlyDictionary<string, string> config = null!;

        // When validating configuration
        var act = async () => await _client.ValidateConfigurationAsync("server1", config, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region ApplyConfigurationAsync Tests

    [Fact]
    public async Task ApplyConfigurationAsync_ShouldApplyConfiguration_WhenValid()
    {
        // Given valid configuration
        var config = new Dictionary<string, string>
        {
            { "ServerName", "New Server Name" },
            { "MaxPlayers", "20" }
        };

        var applyOutput = @"Applying configuration...
Changed settings: ServerName, MaxPlayers
Configuration applied successfully
Restart required: Yes
Server was restarted: No
Backup created: Yes";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, applyOutput, ""));

        // When applying configuration
        var result = await _client.ApplyConfigurationAsync("server1", config, null, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.ChangedSettings.Should().Contain("ServerName");
        result.Value.ChangedSettings.Should().Contain("MaxPlayers");
        result.Value.RequiredRestart.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ShouldApplyWithRestart_WhenOptionSpecified()
    {
        // Given configuration and restart option
        var config = new Dictionary<string, string>
        {
            { "Port", "8212" }
        };
        var options = new ApplyConfigurationOptions(
            ValidateBeforeApply: true,
            BackupBeforeApply: true,
            RestartIfNeeded: true,
            DryRun: false
        );

        var applyOutput = @"Applying configuration...
Validation passed
Backup created: backup_123
Configuration applied successfully
Server restarted";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, applyOutput, ""));

        // When applying configuration with options
        var result = await _client.ApplyConfigurationAsync("server1", config, options, CancellationToken.None);

        // Then the result should be successful with restart
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.WasRestarted.Should().BeTrue();
        result.Value.BackupCreated.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a command that fails
        var config = new Dictionary<string, string> { { "ServerName", "Test" } };

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Failed to apply configuration"));

        // When applying configuration
        var result = await _client.ApplyConfigurationAsync("server1", config, null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to apply configuration");
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var config = new Dictionary<string, string> { { "ServerName", "Test" } };

        // When applying configuration
        var act = async () => await _client.ApplyConfigurationAsync("", config, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyConfigurationAsync_ShouldHandleCancellation()
    {
        // Given a cancelled operation
        var config = new Dictionary<string, string> { { "ServerName", "Test" } };

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // When applying configuration
        var result = await _client.ApplyConfigurationAsync("server1", config, null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancel");
    }

    #endregion
}
