using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.RestartInstance;

/// <summary>
/// Tests for RestartInstanceHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class RestartInstanceHandlerTests
{
    private readonly FakePokManagerClient _fakeClient = new();
    private readonly InMemoryOperationLockManager _lockManager = new();
    private readonly InMemoryAuditSink _auditSink = new();
    private readonly FakeClock _clock = new();
    private RestartInstanceHandler _handler = null!;
    private RestartInstanceRequest _request = null!;
    private Domain.Common.Result<RestartInstanceResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new RestartInstanceHandler(_fakeClient, _lockManager, _auditSink, _clock);
    }

    [Fact]
    public async Task Given_RunningInstance_When_Restart_Then_InstanceRestarts()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenResponseContainsInstanceId("test-instance");
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.RestartInstanceAsync));
        ThenInstanceRemainsRunning("test-instance");
    }

    [Fact]
    public async Task Given_StoppedInstance_When_Restart_Then_ReturnsError()
    {
        // Given
        GivenValidStoppedInstance("stopped-instance");

        // When
        await WhenRestartIsCalled("stopped-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("cannot restart a stopped instance");
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_Restart_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalledWithInvalidId("");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public async Task Given_WithGracePeriod_When_Restart_Then_UsesGracePeriod()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalledWithOptions("test-instance", new RestartInstanceOptions(
            Graceful: true,
            SaveWorld: false,
            WaitForHealthy: false,
            Timeout: TimeSpan.FromSeconds(60)
        ));

        // Then
        ThenResultIsSuccess();
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.RestartInstanceAsync));
    }

    [Fact]
    public async Task Given_WithSaveWorld_When_Restart_Then_SavesBeforeRestart()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalledWithOptions("test-instance", new RestartInstanceOptions(
            Graceful: true,
            SaveWorld: true,
            WaitForHealthy: false,
            Timeout: null
        ));

        // Then
        ThenResultIsSuccess();
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.RestartInstanceAsync));
    }

    [Fact]
    public async Task Given_WithWaitForHealthy_When_Restart_Then_WaitsForHealthy()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalledWithOptions("test-instance", new RestartInstanceOptions(
            Graceful: true,
            SaveWorld: false,
            WaitForHealthy: true,
            Timeout: null
        ));

        // Then
        ThenResultIsSuccess();
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.RestartInstanceAsync));
    }

    [Fact]
    public async Task Given_ValidRequest_When_Restart_Then_AcquiresLock()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenLockWasAcquiredAndReleased("test-instance");
    }

    [Fact]
    public async Task Given_SuccessfulRestart_When_Restart_Then_CreatesAuditEvent()
    {
        // Given
        GivenValidRunningInstance("test-instance");

        // When
        await WhenRestartIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenAuditEventWasCreated("test-instance", "RestartInstance");
    }

    [Fact]
    public async Task Given_InstanceLocked_When_Restart_Then_ReturnsLockFailure()
    {
        // Given
        GivenValidRunningInstance("test-instance");
        await GivenInstanceIsLocked("test-instance");

        // When
        await WhenRestartIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("locked");
    }

    [Fact]
    public async Task Given_PokManagerFails_When_Restart_Then_ReturnsFailureAndCreatesAuditEvent()
    {
        // Given
        GivenValidRunningInstance("test-instance");
        GivenPokManagerWillFail("POK Manager service unavailable");

        // When
        await WhenRestartIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("POK Manager service unavailable");
        ThenAuditEventWasCreatedWithFailure("test-instance", "RestartInstance");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_Restart_Then_ReturnsNotFoundFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalled("nonexistent-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("InstanceNotFound");
    }

    [Fact]
    public async Task Given_InvalidInstanceIdFormat_When_Restart_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalledWithInvalidId("invalid@instance#id");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must contain only alphanumeric characters");
    }

    [Fact]
    public async Task Given_EmptyCorrelationId_When_Restart_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalledWithEmptyCorrelationId("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Correlation ID cannot be empty");
    }

    [Fact]
    public async Task Given_NegativeGracePeriodSeconds_When_Restart_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalledWithGracePeriod("test-instance", -1);

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Grace period must be greater than or equal to 0 seconds");
    }

    [Fact]
    public async Task Given_GracePeriodOver300_When_Restart_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenRestartIsCalledWithGracePeriod("test-instance", 301);

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Grace period must not exceed 300 seconds");
    }

    // Given steps
    private void GivenHandlerIsInitialized()
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        InitializeHandler();
    }

    private void GivenValidRunningInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        InitializeHandler();
    }

    private void GivenValidStoppedInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        InitializeHandler();
    }

    private void GivenPokManagerWillFail(string errorMessage)
    {
        _fakeClient.FailNextOperation(errorMessage);
    }

    private async Task GivenInstanceIsLocked(string instanceId)
    {
        await _lockManager.AcquireLockAsync(instanceId, "other-operation", TimeSpan.FromSeconds(30));
    }

    // When steps
    private async Task WhenRestartIsCalled(string instanceId)
    {
        _request = new RestartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenRestartIsCalledWithInvalidId(string instanceId)
    {
        _request = new RestartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenRestartIsCalledWithEmptyCorrelationId(string instanceId)
    {
        _request = new RestartInstanceRequest(instanceId, "");
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenRestartIsCalledWithOptions(string instanceId, RestartInstanceOptions options)
    {
        _request = new RestartInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            GracePeriodSeconds: options.Graceful ? 30 : 0,
            SaveWorld: options.SaveWorld,
            WaitForHealthy: options.WaitForHealthy);
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenRestartIsCalledWithGracePeriod(string instanceId, int gracePeriodSeconds)
    {
        _request = new RestartInstanceRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            GracePeriodSeconds: gracePeriodSeconds);
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

    private void ThenResponseContainsInstanceId(string expectedId)
    {
        var response = _result.Value;
        response.Should().NotBeNull();
        response.InstanceId.Should().Be(expectedId);
    }

    private void ThenErrorContains(string expectedError)
    {
        _result.Error.Should().ContainEquivalentOf(expectedError);
    }

    private void ThenClientMethodWasCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeTrue();
    }

    private void ThenInstanceRemainsRunning(string instanceId)
    {
        var statusResult = _fakeClient.GetInstanceStatusAsync(instanceId).Result;
        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Value.State.Should().Be(InstanceState.Running);
    }

    private void ThenLockWasAcquiredAndReleased(string instanceId)
    {
        var isLocked = _lockManager.IsLockedAsync(instanceId).Result;
        isLocked.Should().BeFalse("lock should be released after operation completes");
    }

    private void ThenAuditEventWasCreated(string instanceId, string operationType)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().ContainSingle(e =>
            e.InstanceId == instanceId &&
            e.OperationType == operationType &&
            e.Outcome == "Success");
    }

    private void ThenAuditEventWasCreatedWithFailure(string instanceId, string operationType)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().ContainSingle(e =>
            e.InstanceId == instanceId &&
            e.OperationType == operationType &&
            e.Outcome == "Failure");
    }
}
