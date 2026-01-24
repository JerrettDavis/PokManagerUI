using FluentAssertions;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.PokManager.PokManager.Parsers;
using PokManager.Application.Models;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Parsers;

/// <summary>
/// Tests for StatusOutputParser using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class StatusOutputParserTests
{
    private readonly StatusOutputParser _parser = new();
    private Result<InstanceStatus> _result = null!;

    [Fact]
    public void Given_RunningInstanceOutput_When_Parse_Then_ReturnsRunningStatus()
    {
        // Given
        var output = "Instance: MyServer, State: Running, Container: abc123, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("MyServer");
        _result.Value.State.Should().Be(InstanceState.Running);
        _result.Value.Health.Should().Be(ProcessHealth.Healthy);
    }

    [Fact]
    public void Given_StoppedInstanceOutput_When_Parse_Then_ReturnsStoppedStatus()
    {
        // Given
        var output = "Instance: MyServer, State: Stopped, Container: none, Health: Unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("MyServer");
        _result.Value.State.Should().Be(InstanceState.Stopped);
        _result.Value.Health.Should().Be(ProcessHealth.Unknown);
    }

    [Fact]
    public void Given_StartingInstanceOutput_When_Parse_Then_ReturnsStartingStatus()
    {
        // Given
        var output = "Instance: TestInstance, State: Starting, Container: def456, Health: Unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("TestInstance");
        _result.Value.State.Should().Be(InstanceState.Starting);
        _result.Value.Health.Should().Be(ProcessHealth.Unknown);
    }

    [Fact]
    public void Given_FailedInstanceOutput_When_Parse_Then_ReturnsFailedStatus()
    {
        // Given
        var output = "Instance: FailedServer, State: Failed, Container: none, Health: Unhealthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("FailedServer");
        _result.Value.State.Should().Be(InstanceState.Failed);
        _result.Value.Health.Should().Be(ProcessHealth.Unhealthy);
    }

    [Fact]
    public void Given_DegradedHealthOutput_When_Parse_Then_ReturnsDegradedHealth()
    {
        // Given
        var output = "Instance: SlowServer, State: Running, Container: ghi789, Health: Degraded";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("SlowServer");
        _result.Value.State.Should().Be(InstanceState.Running);
        _result.Value.Health.Should().Be(ProcessHealth.Degraded);
    }

    [Fact]
    public void Given_InstanceWithUnderscores_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "Instance: my_test_server_01, State: Running, Container: xyz, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("my_test_server_01");
    }

    [Fact]
    public void Given_InstanceWithHyphens_When_Parse_Then_ParsesCorrectly()
    {
        // Given
        var output = "Instance: my-test-server-01, State: Running, Container: xyz, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("my-test-server-01");
    }

    [Fact]
    public void Given_OutputWithExtraWhitespace_When_Parse_Then_HandlesWhitespace()
    {
        // Given
        var output = "Instance:  MyServer  ,  State:  Running  ,  Container:  abc123  ,  Health:  Healthy  ";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.InstanceId.Should().Be("MyServer");
        _result.Value.State.Should().Be(InstanceState.Running);
        _result.Value.Health.Should().Be(ProcessHealth.Healthy);
    }

    [Fact]
    public void Given_MalformedOutput_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "This is not valid output";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Failed to parse");
    }

    [Fact]
    public void Given_EmptyString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("empty");
    }

    [Fact]
    public void Given_NullString_When_Parse_Then_ReturnsFailure()
    {
        // Given
        string output = null!;

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("null or empty");
    }

    [Fact]
    public void Given_MissingInstanceField_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "State: Running, Container: abc123, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("parse");
    }

    [Fact]
    public void Given_MissingStateField_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Instance: MyServer, Container: abc123, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("parse");
    }

    [Fact]
    public void Given_InvalidStateValue_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Instance: MyServer, State: InvalidState, Container: abc123, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Invalid State value");
    }

    [Fact]
    public void Given_InvalidHealthValue_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Instance: MyServer, State: Running, Container: abc123, Health: InvalidHealth";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Invalid Health value");
    }

    [Fact]
    public void Given_ErrorMessageFromPOKManager_When_Parse_Then_ReturnsFailure()
    {
        // Given
        var output = "Error: Instance not found";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsFailure.Should().BeTrue();
        _result.Error.Should().Contain("Instance not found");
    }

    [Fact]
    public void Given_RestartingState_When_Parse_Then_ReturnsRestartingStatus()
    {
        // Given
        var output = "Instance: RestartServer, State: Restarting, Container: rst123, Health: Unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.State.Should().Be(InstanceState.Restarting);
    }

    [Fact]
    public void Given_StoppingState_When_Parse_Then_ReturnsStoppingStatus()
    {
        // Given
        var output = "Instance: StoppingServer, State: Stopping, Container: stp123, Health: Unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.State.Should().Be(InstanceState.Stopping);
    }

    [Fact]
    public void Given_CreatedState_When_Parse_Then_ReturnsCreatedStatus()
    {
        // Given
        var output = "Instance: NewServer, State: Created, Container: none, Health: Unknown";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.State.Should().Be(InstanceState.Created);
    }

    [Fact]
    public void Given_LowercaseState_When_Parse_Then_ParsesCaseInsensitive()
    {
        // Given
        var output = "Instance: MyServer, State: running, Container: abc123, Health: Healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.State.Should().Be(InstanceState.Running);
    }

    [Fact]
    public void Given_LowercaseHealth_When_Parse_Then_ParsesCaseInsensitive()
    {
        // Given
        var output = "Instance: MyServer, State: Running, Container: abc123, Health: healthy";

        // When
        _result = _parser.Parse(output);

        // Then
        _result.IsSuccess.Should().BeTrue();
        _result.Value.Health.Should().Be(ProcessHealth.Healthy);
    }
}
