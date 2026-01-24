using FluentAssertions;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceLifecycle.StopInstance;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;
using PokManager.Application.Models;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.StopInstance;

/// <summary>
/// Tests for StopInstanceHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class StopInstanceHandlerTests
{
    private readonly FakePokManagerClient _fakeClient = new();
    private readonly InMemoryOperationLockManager _lockManager = new();
    private readonly InMemoryAuditSink _auditSink = new();
    private readonly FakeClock _clock = new();
    private StopInstanceHandler _handler = null!;
    private Result<StopInstanceResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new StopInstanceHandler(_fakeClient, _lockManager, _auditSink, _clock);
    }

    [Fact]
    public async Task Given_RunningInstance_When_Stop_Then_InstanceStops()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceIdIs("island_main");
        ThenTheClientMethodWasCalled(nameof(IPokManagerClient.StopInstanceAsync));
    }

    [Fact]
    public async Task Given_AlreadyStoppedInstance_When_Stop_Then_ReturnsError()
    {
        // Given
        GivenAStoppedInstance("island_main");
        GivenClientWillFailWithError("InstanceAlreadyStopped");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("InstanceAlreadyStopped");
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_Stop_Then_ReturnsValidationFailure()
    {
        // Given
        GivenNoInstancesExist();

        // When
        await WhenStopInstanceIsCalledWithInvalidId("");

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_Stop_Then_ReturnsNotFound()
    {
        // Given
        GivenNoInstancesExist();
        GivenClientWillFailWithError("InstanceNotFound");

        // When
        await WhenStopInstanceIsCalled("nonexistent");

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("InstanceNotFound");
    }

    [Fact]
    public async Task Given_WithGracePeriod_When_Stop_Then_UsesGracePeriod()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalledWithTimeout("island_main", 60);

        // Then
        ThenTheResultIsSuccess();
        ThenTheClientMethodWasCalled(nameof(IPokManagerClient.StopInstanceAsync));
    }

    [Fact]
    public async Task Given_WithForceKill_When_Stop_Then_ForcesStop()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalledWithForceKill("island_main", forceKill: true);

        // Then
        ThenTheResultIsSuccess();
        ThenTheClientMethodWasCalled(nameof(IPokManagerClient.StopInstanceAsync));
    }

    [Fact]
    public async Task Given_ValidRequest_When_Stop_Then_AcquiresLock()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsSuccess();
        ThenTheLockWasReleased("island_main");
    }

    [Fact]
    public async Task Given_InstanceLocked_When_Stop_Then_ReturnsLockFailure()
    {
        // Given
        GivenARunningInstance("island_main");
        await GivenInstanceIsLocked("island_main", "other-operation-id");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("locked");
    }

    [Fact]
    public async Task Given_SuccessfulStop_When_Stop_Then_CreatesAuditEvent()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsSuccess();
        ThenAnAuditEventWasCreated();
        ThenTheAuditEventHasOperationType("StopInstance");
        ThenTheAuditEventHasInstanceId("island_main");
        ThenTheAuditEventHasOutcome("Success");
    }

    [Fact]
    public async Task Given_FailedStop_When_Stop_Then_CreatesFailureAuditEvent()
    {
        // Given
        GivenARunningInstance("island_main");
        GivenClientWillFailWithError("StopFailed");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsFailure();
        ThenAnAuditEventWasCreated();
        ThenTheAuditEventHasOperationType("StopInstance");
        ThenTheAuditEventHasOutcome("Failure");
        ThenTheAuditEventHasErrorMessage("StopFailed");
    }

    [Fact]
    public async Task Given_ValidRequestWithCorrelationId_When_Stop_Then_UsesCorrelationId()
    {
        // Given
        var correlationId = Guid.NewGuid().ToString();
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalledWithCorrelationId("island_main", correlationId);

        // Then
        ThenTheResultIsSuccess();
        ThenTheInstanceIdIs("island_main");
    }

    [Fact]
    public async Task Given_StopInstanceSuccess_When_Stop_Then_ReleasesLockOnCompletion()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsSuccess();
        ThenTheLockWasReleased("island_main");
    }

    [Fact]
    public async Task Given_StopInstanceFailure_When_Stop_Then_ReleasesLockOnFailure()
    {
        // Given
        GivenARunningInstance("island_main");
        GivenClientWillFailWithError("StopFailed");

        // When
        await WhenStopInstanceIsCalled("island_main");

        // Then
        ThenTheResultIsFailure();
        ThenTheLockWasReleased("island_main");
    }

    [Fact]
    public async Task Given_InvalidInstanceIdFormat_When_Stop_Then_ReturnsValidationError()
    {
        // Given
        GivenNoInstancesExist();

        // When
        await WhenStopInstanceIsCalledWithInvalidId("invalid instance id!");

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("must contain only alphanumeric characters");
    }

    [Fact]
    public async Task Given_TimeoutExceedsMaximum_When_Stop_Then_ReturnsValidationError()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalledWithTimeout("island_main", 400);

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("must not exceed 300 seconds");
    }

    [Fact]
    public async Task Given_NegativeTimeout_When_Stop_Then_ReturnsValidationError()
    {
        // Given
        GivenARunningInstance("island_main");

        // When
        await WhenStopInstanceIsCalledWithTimeout("island_main", -10);

        // Then
        ThenTheResultIsFailure();
        ThenTheErrorContains("must be greater than 0 seconds");
    }

    #region Given Steps

    private void GivenNoInstancesExist()
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        InitializeHandler();
    }

    private void GivenARunningInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        InitializeHandler();
    }

    private void GivenAStoppedInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        InitializeHandler();
    }

    private void GivenClientWillFailWithError(string errorMessage)
    {
        _fakeClient.FailNextOperation(errorMessage);
    }

    private async Task GivenInstanceIsLocked(string instanceId, string operationId)
    {
        var lockResult = await _lockManager.AcquireLockAsync(
            instanceId,
            operationId,
            TimeSpan.FromSeconds(30));
        lockResult.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region When Steps

    private async Task WhenStopInstanceIsCalled(string instanceId)
    {
        var request = new StopInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString());
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    private async Task WhenStopInstanceIsCalledWithInvalidId(string instanceId)
    {
        var request = new StopInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString());
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    private async Task WhenStopInstanceIsCalledWithTimeout(string instanceId, int timeoutSeconds)
    {
        var request = new StopInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            ForceKill: false,
            TimeoutSeconds: timeoutSeconds);
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    private async Task WhenStopInstanceIsCalledWithForceKill(string instanceId, bool forceKill)
    {
        var request = new StopInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            ForceKill: forceKill);
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    private async Task WhenStopInstanceIsCalledWithCorrelationId(string instanceId, string correlationId)
    {
        var request = new StopInstanceRequest(
            instanceId,
            correlationId);
        _result = await _handler.Handle(request, CancellationToken.None);
    }

    #endregion

    #region Then Steps

    private void ThenTheResultIsSuccess()
    {
        _result.Should().NotBeNull();
        _result.IsSuccess.Should().BeTrue();
    }

    private void ThenTheResultIsFailure()
    {
        _result.Should().NotBeNull();
        _result.IsFailure.Should().BeTrue();
    }

    private void ThenTheErrorContains(string expectedError)
    {
        _result.Error.Should().Contain(expectedError);
    }

    private void ThenTheInstanceIdIs(string expectedInstanceId)
    {
        _result.Value.InstanceId.Should().Be(expectedInstanceId);
    }

    private void ThenTheClientMethodWasCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeTrue();
    }

    private void ThenTheLockWasReleased(string instanceId)
    {
        var isLocked = _lockManager.IsLockedAsync(instanceId).Result;
        isLocked.Should().BeFalse();
    }

    private void ThenAnAuditEventWasCreated()
    {
        var events = _auditSink.GetAllEvents();
        events.Should().NotBeEmpty();
    }

    private void ThenTheAuditEventHasOperationType(string operationType)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.OperationType == operationType);
    }

    private void ThenTheAuditEventHasInstanceId(string instanceId)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.InstanceId == instanceId);
    }

    private void ThenTheAuditEventHasOutcome(string outcome)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.Outcome == outcome);
    }

    private void ThenTheAuditEventHasErrorMessage(string errorMessage)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.ErrorMessage != null && e.ErrorMessage.Contains(errorMessage));
    }

    #endregion
}
