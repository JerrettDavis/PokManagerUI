using FluentAssertions;
using PokManager.Application.UseCases.InstanceQuery;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceQuery;

/// <summary>
/// Tests for GetInstanceStatusHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class GetInstanceStatusHandlerTests
{
    private readonly FakePokManagerClient _fakeClient = new();
    private GetInstanceStatusHandler _handler = null!;
    private GetInstanceStatusRequest _request = null!;
    private Domain.Common.Result<GetInstanceStatusResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new GetInstanceStatusHandler(_fakeClient);
    }

    [Fact]
    public async Task Given_ValidInstanceId_When_GetStatus_Then_ReturnsStatus()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenGetStatusIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenResponseContainsValidStatus();
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_GetStatus_Then_ReturnsFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenGetStatusIsCalledWithInvalidId("");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_GetStatus_Then_ReturnsNotFoundFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenGetStatusIsCalled("nonexistent-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("InstanceNotFound");
    }

    [Fact]
    public async Task Given_PokManagerFails_When_GetStatus_Then_ReturnsFailure()
    {
        // Given
        GivenPokManagerWillFail("POK Manager service unavailable");

        // When
        await WhenGetStatusIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("POK Manager service unavailable");
    }

    [Fact]
    public async Task Given_RunningInstance_When_GetStatus_Then_IncludesAllDetails()
    {
        // Given
        GivenValidRunningInstance("running-instance");

        // When
        await WhenGetStatusIsCalled("running-instance");

        // Then
        ThenResultIsSuccess();
        ThenStatusIncludesInstanceId("running-instance");
        ThenStatusIncludesState(InstanceState.Running);
        ThenStatusIncludesHealth();
        ThenStatusIncludesUptime();
    }

    [Fact]
    public async Task Given_StoppedInstance_When_GetStatus_Then_ReturnsStoppedState()
    {
        // Given
        GivenValidStoppedInstance("stopped-instance");

        // When
        await WhenGetStatusIsCalled("stopped-instance");

        // Then
        ThenResultIsSuccess();
        ThenStatusIncludesState(InstanceState.Stopped);
    }

    [Fact]
    public async Task Given_InstanceIdTooLong_When_GetStatus_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenGetStatusIsCalledWithInvalidId(new string('a', 65));

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must be maximum 64 characters");
    }

    [Fact]
    public async Task Given_InstanceIdWithInvalidCharacters_When_GetStatus_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenGetStatusIsCalledWithInvalidId("invalid@instance#id");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must contain only alphanumeric characters");
    }

    [Fact]
    public async Task Given_EmptyCorrelationId_When_GetStatus_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenGetStatusIsCalledWithEmptyCorrelationId("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Correlation ID cannot be empty");
    }

    [Fact]
    public async Task Given_ValidRequest_When_GetStatus_Then_CallsPokManagerClient()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenGetStatusIsCalled("test-instance");

        // Then
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.GetInstanceStatusAsync));
    }

    [Fact]
    public async Task Given_StartingInstance_When_GetStatus_Then_ReturnsStartingState()
    {
        // Given
        GivenInstanceInState("starting-instance", InstanceState.Starting);

        // When
        await WhenGetStatusIsCalled("starting-instance");

        // Then
        ThenResultIsSuccess();
        ThenStatusIncludesState(InstanceState.Starting);
    }

    [Fact]
    public async Task Given_FailedInstance_When_GetStatus_Then_ReturnsFailedState()
    {
        // Given
        GivenInstanceInState("failed-instance", InstanceState.Failed);

        // When
        await WhenGetStatusIsCalled("failed-instance");

        // Then
        ThenResultIsSuccess();
        ThenStatusIncludesState(InstanceState.Failed);
    }

    // Given steps
    private void GivenHandlerIsInitialized()
    {
        _fakeClient.Reset();
        InitializeHandler();
    }

    private void GivenValidRunningInstance(string instanceId)
    {
        _fakeClient.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        InitializeHandler();
    }

    private void GivenValidStoppedInstance(string instanceId)
    {
        _fakeClient.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        InitializeHandler();
    }

    private void GivenInstanceInState(string instanceId, InstanceState state)
    {
        _fakeClient.Reset();
        _fakeClient.SetupInstance(instanceId, state);
        InitializeHandler();
    }

    private void GivenPokManagerWillFail(string errorMessage)
    {
        _fakeClient.Reset();
        _fakeClient.FailNextOperation(errorMessage);
        InitializeHandler();
    }

    // When steps
    private async Task WhenGetStatusIsCalled(string instanceId)
    {
        _request = new GetInstanceStatusRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenGetStatusIsCalledWithInvalidId(string instanceId)
    {
        _request = new GetInstanceStatusRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenGetStatusIsCalledWithEmptyCorrelationId(string instanceId)
    {
        _request = new GetInstanceStatusRequest(instanceId, "");
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    // Then steps
    private void ThenResultIsSuccess()
    {
        _result.Should().NotBeNull();
        _result.IsSuccess.Should().BeTrue();
    }

    private void ThenResultIsFailure()
    {
        _result.Should().NotBeNull();
        _result.IsFailure.Should().BeTrue();
    }

    private void ThenResponseContainsValidStatus()
    {
        var response = _result.Value;
        response.Should().NotBeNull();
        response.Status.Should().NotBeNull();
    }

    private void ThenErrorContains(string expectedError)
    {
        _result.Error.Should().Contain(expectedError);
    }

    private void ThenStatusIncludesInstanceId(string expectedId)
    {
        var response = _result.Value;
        response.Status.Id.Should().Be(expectedId);
    }

    private void ThenStatusIncludesState(InstanceState expectedState)
    {
        var response = _result.Value;
        response.Status.State.Should().Be(expectedState);
    }

    private void ThenStatusIncludesHealth()
    {
        var response = _result.Value;
        response.Status.Health.Should().NotBe(ProcessHealth.Unknown);
    }

    private void ThenStatusIncludesUptime()
    {
        var response = _result.Value;
        response.Status.Uptime.Should().NotBeNull();
    }

    private void ThenClientMethodWasCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeTrue();
    }
}
