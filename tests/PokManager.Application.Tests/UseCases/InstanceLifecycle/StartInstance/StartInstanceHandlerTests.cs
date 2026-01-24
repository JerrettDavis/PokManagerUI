using FluentAssertions;
using NSubstitute;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.InstanceLifecycle.StartInstance;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceLifecycle.StartInstance;

/// <summary>
/// Tests for StartInstanceHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class StartInstanceHandlerTests
{
    private readonly FakePokManagerClient _fakeClient = new();
    private readonly InMemoryOperationLockManager _fakeLockManager = new();
    private readonly InMemoryAuditSink _fakeAuditSink = new();
    private readonly FakeClock _fakeClock = new();
    private readonly ICacheInvalidationService _fakeCacheInvalidation = Substitute.For<ICacheInvalidationService>();
    private StartInstanceHandler _handler = null!;
    private StartInstanceRequest _request = null!;
    private Result<StartInstanceResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new StartInstanceHandler(
            _fakeClient,
            _fakeLockManager,
            _fakeAuditSink,
            _fakeClock,
            _fakeCacheInvalidation);
    }

    [Fact]
    public async Task Given_StoppedInstance_When_Start_Then_InstanceStarts()
    {
        // Given
        GivenStoppedInstance("test-instance");

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenResponseContainsInstanceId("test-instance");
        ThenInstanceIsInState(InstanceState.Running);
        ThenPokManagerClientWasCalled(nameof(FakePokManagerClient.StartInstanceAsync));
    }

    [Fact]
    public async Task Given_AlreadyRunningInstance_When_Start_Then_ReturnsError()
    {
        // Given
        GivenRunningInstance("running-instance");

        // When
        await WhenStartInstanceIsCalled("running-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("InstanceAlreadyRunning");
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_Start_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenStartInstanceIsCalledWithInvalidId("");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_Start_Then_ReturnsNotFound()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenStartInstanceIsCalled("nonexistent-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("InstanceNotFound");
    }

    [Fact]
    public async Task Given_PokManagerFails_When_Start_Then_ReturnsFailure()
    {
        // Given
        GivenStoppedInstanceButPokManagerWillFail("test-instance", "POK Manager service unavailable");

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("POK Manager service unavailable");
    }

    [Fact]
    public async Task Given_ValidRequest_When_Start_Then_AcquiresLock()
    {
        // Given
        GivenStoppedInstance("test-instance");

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenLockWasAcquiredAndReleased("test-instance");
    }

    [Fact]
    public async Task Given_InstanceLocked_When_Start_Then_ReturnsLockFailure()
    {
        // Given
        GivenStoppedInstanceThatIsLocked("locked-instance");

        // When
        await WhenStartInstanceIsCalled("locked-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("locked by operation");
    }

    [Fact]
    public async Task Given_SuccessfulStart_When_Start_Then_CreatesAuditEvent()
    {
        // Given
        GivenStoppedInstance("test-instance");
        var startTime = DateTimeOffset.UtcNow;
        _fakeClock.SetTime(startTime);

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenAuditEventWasCreated("test-instance", "StartInstance", "Success");
    }

    [Fact]
    public async Task Given_InstanceIdTooLong_When_Start_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenStartInstanceIsCalledWithInvalidId(new string('a', 65));

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must be maximum 64 characters");
    }

    [Fact]
    public async Task Given_InstanceIdWithInvalidCharacters_When_Start_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenStartInstanceIsCalledWithInvalidId("invalid@instance#id");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must contain only alphanumeric characters");
    }

    [Fact]
    public async Task Given_EmptyCorrelationId_When_Start_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenStartInstanceIsCalledWithEmptyCorrelationId("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Correlation ID cannot be empty");
    }

    [Fact]
    public async Task Given_FailedStart_When_Start_Then_CreatesFailureAuditEvent()
    {
        // Given
        GivenStoppedInstanceButPokManagerWillFail("test-instance", "Start operation failed");
        var startTime = DateTimeOffset.UtcNow;
        _fakeClock.SetTime(startTime);

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenAuditEventWasCreated("test-instance", "StartInstance", "Failure");
    }

    [Fact]
    public async Task Given_LockAcquisitionFails_When_Start_Then_DoesNotCallPokManager()
    {
        // Given
        GivenStoppedInstanceThatIsLocked("locked-instance");

        // When
        await WhenStartInstanceIsCalled("locked-instance");

        // Then
        ThenResultIsFailure();
        ThenPokManagerClientWasNotCalled(nameof(FakePokManagerClient.StartInstanceAsync));
    }

    [Fact]
    public async Task Given_ValidRequest_When_Start_Then_ChecksInstanceStateBeforeStarting()
    {
        // Given
        GivenStoppedInstance("test-instance");

        // When
        await WhenStartInstanceIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenPokManagerClientWasCalled(nameof(FakePokManagerClient.GetInstanceStatusAsync));
    }

    // Given steps
    private void GivenHandlerIsInitialized()
    {
        _fakeClient.Reset();
        _fakeLockManager.Reset();
        _fakeAuditSink.Reset();
        _fakeClock.Reset();
        InitializeHandler();
    }

    private void GivenStoppedInstance(string instanceId)
    {
        _fakeClient.Reset();
        _fakeLockManager.Reset();
        _fakeAuditSink.Reset();
        _fakeClock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        InitializeHandler();
    }

    private void GivenRunningInstance(string instanceId)
    {
        _fakeClient.Reset();
        _fakeLockManager.Reset();
        _fakeAuditSink.Reset();
        _fakeClock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        InitializeHandler();
    }

    private void GivenStoppedInstanceButPokManagerWillFail(string instanceId, string errorMessage)
    {
        _fakeClient.Reset();
        _fakeLockManager.Reset();
        _fakeAuditSink.Reset();
        _fakeClock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        _fakeClient.FailNextOperation(errorMessage);
        InitializeHandler();
    }

    private async void GivenStoppedInstanceThatIsLocked(string instanceId)
    {
        _fakeClient.Reset();
        _fakeLockManager.Reset();
        _fakeAuditSink.Reset();
        _fakeClock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        // Pre-acquire the lock with a different operation
        await _fakeLockManager.AcquireLockAsync(instanceId, "other-operation", TimeSpan.FromMinutes(1));
        InitializeHandler();
    }

    // When steps
    private async Task WhenStartInstanceIsCalled(string instanceId)
    {
        _request = new StartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenStartInstanceIsCalledWithInvalidId(string instanceId)
    {
        _request = new StartInstanceRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenStartInstanceIsCalledWithEmptyCorrelationId(string instanceId)
    {
        _request = new StartInstanceRequest(instanceId, "");
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

    private void ThenInstanceIsInState(InstanceState expectedState)
    {
        var statusResult = _fakeClient.GetInstanceStatusAsync(_request.InstanceId).Result;
        statusResult.IsSuccess.Should().BeTrue();
        statusResult.Value.State.Should().Be(expectedState);
    }

    private void ThenErrorContains(string expectedError)
    {
        _result.Error.Should().Contain(expectedError);
    }

    private void ThenPokManagerClientWasCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeTrue();
    }

    private void ThenPokManagerClientWasNotCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeFalse();
    }

    private void ThenLockWasAcquiredAndReleased(string instanceId)
    {
        // After operation completes, lock should be released
        var isLocked = _fakeLockManager.IsLockedAsync(instanceId).Result;
        isLocked.Should().BeFalse("lock should be released after operation completes");
    }

    private void ThenAuditEventWasCreated(string instanceId, string operationType, string outcome)
    {
        var events = _fakeAuditSink.GetAllEvents();
        events.Should().NotBeEmpty();

        var matchingEvent = events.FirstOrDefault(e =>
            e.InstanceId == instanceId &&
            e.OperationType == operationType &&
            e.Outcome == outcome);

        matchingEvent.Should().NotBeNull($"audit event with instance {instanceId}, operation {operationType}, and outcome {outcome} should exist");
    }
}
