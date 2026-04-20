using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PokManager.Application.Ports;
using PokManager.Infrastructure.PokManager;
using PokManager.Infrastructure.Shell;
using Xunit;
using static TinyBDD.TestContext;

namespace PokManager.Infrastructure.Tests.PokManager;

/// <summary>
/// Tests for PokManagerClient following TDD principles.
/// Uses mocked IBashCommandExecutor to test the client behavior.
/// </summary>
public class PokManagerClientTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientTests()
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

    #region ListInstancesAsync Tests

    [Fact]
    public void ListInstancesAsync_ShouldReturnEmptyList_WhenNoInstancesExist()
    {
        TestContext.Run
            .Given("an instances directory with no Instance_* subdirectories", () =>
            {
                // Mock directory listing command that returns no instances
                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, "", ""));
            })
            .When("listing instances", async () =>
            {
                return await _client.ListInstancesAsync(CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("the list should be empty", (result) =>
            {
                result.Value.Should().BeEmpty();
            })
            .Run();
    }

    [Fact]
    public void ListInstancesAsync_ShouldReturnInstanceIds_WhenInstancesExist()
    {
        TestContext.Run
            .Given("an instances directory with multiple Instance_* subdirectories", () =>
            {
                // Simulate directory output with instance directories
                var directoryOutput = @"Instance_server1
Instance_server2
Instance_test";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, directoryOutput, ""));
            })
            .When("listing instances", async () =>
            {
                return await _client.ListInstancesAsync(CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("should return all instance IDs", (result) =>
            {
                result.Value.Should().HaveCount(3);
                result.Value.Should().Contain("server1");
                result.Value.Should().Contain("server2");
                result.Value.Should().Contain("test");
            })
            .Run();
    }

    [Fact]
    public void ListInstancesAsync_ShouldIgnoreNonInstanceDirectories()
    {
        TestContext.Run
            .Given("a directory with mixed content", () =>
            {
                var directoryOutput = @"Instance_server1
backups
logs
Instance_server2
some_other_dir";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, directoryOutput, ""));
            })
            .When("listing instances", async () =>
            {
                return await _client.ListInstancesAsync(CancellationToken.None);
            })
            .Then("should only return Instance_* directories", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
                result.Value.Should().HaveCount(2);
                result.Value.Should().Contain("server1");
                result.Value.Should().Contain("server2");
            })
            .Run();
    }

    [Fact]
    public async Task ListInstancesAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Directory not found"));

        // When listing instances
        var result = await _client.ListInstancesAsync(CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to list instances");
    }

    #endregion

    #region GetInstanceStatusAsync Tests

    [Fact]
    public void GetInstanceStatusAsync_ShouldReturnStatus_WhenInstanceIsRunning()
    {
        TestContext.Run
            .Given("a running instance with status output", () =>
            {
                var statusOutput = @"Container: abc123
Status: running
Health: healthy
Uptime: 2 hours";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, statusOutput, ""));

                return "server1";
            })
            .When("getting instance status", async (instanceId) =>
            {
                return await _client.GetInstanceStatusAsync(instanceId, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("should contain correct status information", (result) =>
            {
                var status = result.Value;
                status.InstanceId.Should().Be("server1");
                status.State.Should().BeDefined();
            })
            .Run();
    }

    [Fact]
    public async Task GetInstanceStatusAsync_ShouldReturnFailure_WhenInstanceNotFound()
    {
        // Given an instance that doesn't exist
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When getting the status
        var result = await _client.GetInstanceStatusAsync("nonexistent", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to get status");
    }

    [Fact]
    public async Task GetInstanceStatusAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When getting the status
        var act = async () => await _client.GetInstanceStatusAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetInstanceDetailsAsync Tests

    [Fact]
    public void GetInstanceDetailsAsync_ShouldReturnDetails_WhenInstanceExists()
    {
        TestContext.Run
            .Given("an instance with details", () =>
            {
                var detailsOutput = @"Instance: server1
Container: abc123
Status: running
Health: healthy
Port: 8211
MaxPlayers: 32
ServerName: My Palworld Server
Version: 0.1.5.1
InstallPath: /opt/pok/instances/Instance_server1
WorldPath: /opt/pok/instances/Instance_server1/Pal/Saved
ConfigPath: /opt/pok/instances/Instance_server1/Pal/Saved/Config";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, detailsOutput, ""));

                return "server1";
            })
            .When("getting instance details", async (instanceId) =>
            {
                return await _client.GetInstanceDetailsAsync(instanceId, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("should contain correct details", (result) =>
            {
                var details = result.Value;
                details.InstanceId.Should().Be("server1");
                details.ServerName.Should().NotBeNullOrEmpty();
            })
            .Run();
    }

    [Fact]
    public async Task GetInstanceDetailsAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When getting instance details
        var result = await _client.GetInstanceDetailsAsync("nonexistent", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ShouldHandleTimeoutGracefully()
    {
        // Given a command that times out
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Command timed out"));

        // When listing instances
        var result = await _client.ListInstancesAsync(CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task ShouldHandleCancellationGracefully()
    {
        // Given a cancelled operation
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // When listing instances
        var result = await _client.ListInstancesAsync(CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancelled");
    }

    #endregion
}
