using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.PokManager;
using PokManager.Infrastructure.Shell;
using static TinyBDD.TestContext;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager;

/// <summary>
/// Tests for PokManagerClient lifecycle management methods following TDD principles.
/// Uses mocked IBashCommandExecutor to test the client behavior.
/// </summary>
public class PokManagerClientLifecycleTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientLifecycleTests()
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

    #region CreateInstanceAsync Tests

    [Fact]
    public void CreateInstanceAsync_ShouldReturnInstanceId_WhenCreationSucceeds()
    {
        TestContext.Run
            .Given("a valid create instance request", () =>
            {
                var request = new CreateInstanceRequest(
                    InstanceId: "testserver",
                    ServerName: "Test Server",
                    Port: 8211,
                    MaxPlayers: 32,
                    ServerPassword: null,
                    AdminPassword: "admin123",
                    AutoStart: false
                );

                var createOutput = @"Creating instance 'testserver'...
Instance created successfully
Instance ID: testserver
Server Name: Test Server
Port: 8211";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, createOutput, ""));

                return request;
            })
            .When("creating the instance", async (request) =>
            {
                return await _client.CreateInstanceAsync(request, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("should return the instance ID", (result) =>
            {
                result.Value.Should().Be("testserver");
            })
            .Run();
    }

    [Fact]
    public async Task CreateInstanceAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a request and a failed command execution
        var request = new CreateInstanceRequest(
            "testserver",
            "Test Server",
            8211,
            32,
            null,
            "admin123"
        );

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Port already in use"));

        // When creating the instance
        var result = await _client.CreateInstanceAsync(request, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to create instance");
    }

    [Fact]
    public async Task CreateInstanceAsync_ShouldHandleTimeout()
    {
        // Given a request and a timeout
        var request = new CreateInstanceRequest(
            "testserver",
            "Test Server",
            8211,
            32,
            null,
            "admin123"
        );

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException("Operation timed out")));

        // When creating the instance
        var result = await _client.CreateInstanceAsync(request, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task CreateInstanceAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Given a null request
        CreateInstanceRequest? request = null;

        // When creating the instance
        var act = async () => await _client.CreateInstanceAsync(request!, CancellationToken.None);

        // Then an ArgumentNullException should be thrown
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region StartInstanceAsync Tests

    [Fact]
    public void StartInstanceAsync_ShouldSucceed_WhenInstanceStarts()
    {
        TestContext.Run
            .Given("a stopped instance", () =>
            {
                var startOutput = @"Starting instance 'testserver'...
Instance started successfully
Status: running";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, startOutput, ""));

                return "testserver";
            })
            .When("starting the instance", async (instanceId) =>
            {
                return await _client.StartInstanceAsync(instanceId, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .Run();
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed start command
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance already running"));

        // When starting the instance
        var result = await _client.StartInstanceAsync("testserver", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to start instance");
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldHandleTimeout()
    {
        // Given a timeout
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException()));

        // When starting the instance
        var result = await _client.StartInstanceAsync("testserver", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task StartInstanceAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When starting the instance
        var act = async () => await _client.StartInstanceAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region StopInstanceAsync Tests

    [Fact]
    public void StopInstanceAsync_ShouldSucceed_WhenInstanceStops()
    {
        TestContext.Run
            .Given("a running instance", () =>
            {
                var stopOutput = @"Stopping instance 'testserver'...
Saving world...
Instance stopped successfully
Status: stopped";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, stopOutput, ""));

                return ("testserver", new StopInstanceOptions(Graceful: true, SaveWorld: true));
            })
            .When("stopping the instance", async (data) =>
            {
                return await _client.StopInstanceAsync(data.Item1, data.Item2, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .Run();
    }

    [Fact]
    public async Task StopInstanceAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed stop command
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not running"));

        // When stopping the instance
        var result = await _client.StopInstanceAsync("testserver", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to stop instance");
    }

    [Fact]
    public async Task StopInstanceAsync_ShouldHandleTimeout()
    {
        // Given a timeout
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException()));

        // When stopping the instance
        var result = await _client.StopInstanceAsync("testserver", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task StopInstanceAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When stopping the instance
        var act = async () => await _client.StopInstanceAsync(instanceId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region RestartInstanceAsync Tests

    [Fact]
    public void RestartInstanceAsync_ShouldSucceed_WhenInstanceRestarts()
    {
        TestContext.Run
            .Given("a running instance", () =>
            {
                var restartOutput = @"Restarting instance 'testserver'...
Saving world...
Stopping instance...
Starting instance...
Instance restarted successfully
Status: running";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, restartOutput, ""));

                return ("testserver", new RestartInstanceOptions(Graceful: true, SaveWorld: true));
            })
            .When("restarting the instance", async (data) =>
            {
                return await _client.RestartInstanceAsync(data.Item1, data.Item2, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .Run();
    }

    [Fact]
    public async Task RestartInstanceAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed restart command
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Failed to restart"));

        // When restarting the instance
        var result = await _client.RestartInstanceAsync("testserver", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to restart instance");
    }

    [Fact]
    public async Task RestartInstanceAsync_ShouldHandleTimeout()
    {
        // Given a timeout
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException()));

        // When restarting the instance
        var result = await _client.RestartInstanceAsync("testserver", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task RestartInstanceAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When restarting the instance
        var act = async () => await _client.RestartInstanceAsync(instanceId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DeleteInstanceAsync Tests

    [Fact]
    public void DeleteInstanceAsync_ShouldSucceed_WhenInstanceDeleted()
    {
        TestContext.Run
            .Given("a stopped instance", () =>
            {
                var deleteOutput = @"Deleting instance 'testserver'...
Removing instance directory...
Instance deleted successfully";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, deleteOutput, ""));

                return "testserver";
            })
            .When("deleting the instance", async (instanceId) =>
            {
                return await _client.DeleteInstanceAsync(instanceId, false, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .Run();
    }

    [Fact]
    public async Task DeleteInstanceAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed delete command
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When deleting the instance
        var result = await _client.DeleteInstanceAsync("testserver", false, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to delete instance");
    }

    [Fact]
    public async Task DeleteInstanceAsync_ShouldHandleTimeout()
    {
        // Given a timeout
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException()));

        // When deleting the instance
        var result = await _client.DeleteInstanceAsync("testserver", false, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task DeleteInstanceAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When deleting the instance
        var act = async () => await _client.DeleteInstanceAsync(instanceId, false, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region HealthCheckAsync Tests

    [Fact]
    public void HealthCheckAsync_ShouldReturnHealthy_WhenInstanceIsHealthy()
    {
        TestContext.Run
            .Given("a healthy instance", () =>
            {
                var healthOutput = @"Checking health of instance 'testserver'...
Status: running
Health: healthy
Response time: 50ms";

                _mockExecutor
                    .ExecuteAsync(
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<TimeSpan>(),
                        Arg.Any<CancellationToken>())
                    .Returns(new BashCommandResult(0, healthOutput, ""));

                return "testserver";
            })
            .When("checking health", async (instanceId) =>
            {
                return await _client.HealthCheckAsync(instanceId, CancellationToken.None);
            })
            .Then("the result should be successful", (result) =>
            {
                result.IsSuccess.Should().BeTrue();
            })
            .And("should indicate healthy status", (result) =>
            {
                result.Value.IsHealthy.Should().BeTrue();
                result.Value.Health.Should().Be(ProcessHealth.Healthy);
                result.Value.InstanceId.Should().Be("testserver");
            })
            .Run();
    }

    [Fact]
    public async Task HealthCheckAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed health check command
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When checking health
        var result = await _client.HealthCheckAsync("testserver", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to check health");
    }

    [Fact]
    public async Task HealthCheckAsync_ShouldHandleTimeout()
    {
        // Given a timeout
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<BashCommandResult>(new TimeoutException()));

        // When checking health
        var result = await _client.HealthCheckAsync("testserver", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task HealthCheckAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When checking health
        var act = async () => await _client.HealthCheckAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
