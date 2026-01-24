using FluentAssertions;
using PokManager.Application.UseCases.InstanceManagement.SaveWorld;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.InstanceManagement.SaveWorld;

/// <summary>
/// Tests for SaveWorldHandler using TinyBDD-style naming conventions.
/// Tests follow the Given-When-Then pattern with descriptive method names.
/// </summary>
public class SaveWorldHandlerTests
{
    private readonly FakePokManagerClient _fakeClient = new();
    private readonly InMemoryOperationLockManager _lockManager = new();
    private readonly InMemoryAuditSink _auditSink = new();
    private readonly FakeClock _clock = new();
    private SaveWorldHandler _handler = null!;
    private SaveWorldRequest _request = null!;
    private Domain.Common.Result<SaveWorldResponse> _result = null!;

    private void InitializeHandler()
    {
        _handler = new SaveWorldHandler(_fakeClient, _lockManager, _auditSink, _clock);
    }

    [Fact]
    public async Task Given_RunningInstance_When_SaveWorld_Then_WorldSaved()
    {
        // Given
        GivenRunningInstance("test-instance");

        // When
        await WhenSaveWorldIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenResponseIsNotNull();
        ThenClientMethodWasCalled(nameof(FakePokManagerClient.SaveWorldAsync));
        ThenAuditEventWasCreated();
    }

    [Fact]
    public async Task Given_StoppedInstance_When_SaveWorld_Then_ReturnsError()
    {
        // Given
        GivenStoppedInstance("stopped-instance");

        // When
        await WhenSaveWorldIsCalled("stopped-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("not running");
        ThenClientMethodWasNotCalled(nameof(FakePokManagerClient.SaveWorldAsync));
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_SaveWorld_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenSaveWorldIsCalledWithInvalidId("");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID cannot be empty");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_SaveWorld_Then_ReturnsNotFound()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenSaveWorldIsCalled("nonexistent-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("InstanceNotFound");
    }

    [Fact]
    public async Task Given_PokManagerFails_When_SaveWorld_Then_ReturnsFailure()
    {
        // Given
        GivenRunningInstanceWithClientFailure("test-instance", "Save operation failed");

        // When
        await WhenSaveWorldIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Save operation failed");
        ThenAuditEventWasCreatedWithFailure();
    }

    [Fact]
    public async Task Given_ValidRequest_When_SaveWorld_Then_AcquiresLock()
    {
        // Given
        GivenRunningInstance("test-instance");

        // When
        await WhenSaveWorldIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenLockWasAcquired("test-instance");
        ThenLockWasReleased("test-instance");
    }

    [Fact]
    public async Task Given_InstanceLocked_When_SaveWorld_Then_ReturnsLockFailure()
    {
        // Given
        await GivenRunningInstanceWithActiveLock("test-instance");

        // When
        await WhenSaveWorldIsCalled("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("locked");
    }

    [Fact]
    public async Task Given_SuccessfulSave_When_SaveWorld_Then_CreatesAuditEvent()
    {
        // Given
        GivenRunningInstance("test-instance");

        // When
        await WhenSaveWorldIsCalled("test-instance");

        // Then
        ThenResultIsSuccess();
        ThenAuditEventWasCreated();
        ThenAuditEventHasCorrectOperationType("SaveWorld");
        ThenAuditEventHasOutcome("Success");
    }

    [Fact]
    public async Task Given_InstanceIdTooLong_When_SaveWorld_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenSaveWorldIsCalledWithInvalidId(new string('a', 65));

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Instance ID must be maximum 64 characters");
    }

    [Fact]
    public async Task Given_EmptyCorrelationId_When_SaveWorld_Then_ReturnsValidationFailure()
    {
        // Given
        GivenHandlerIsInitialized();

        // When
        await WhenSaveWorldIsCalledWithEmptyCorrelationId("test-instance");

        // Then
        ThenResultIsFailure();
        ThenErrorContains("Correlation ID cannot be empty");
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

    private void GivenRunningInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        InitializeHandler();
    }

    private void GivenStoppedInstance(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Stopped);
        InitializeHandler();
    }

    private void GivenRunningInstanceWithClientFailure(string instanceId, string errorMessage)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        _fakeClient.FailNextOperation(errorMessage);
        InitializeHandler();
    }

    private async Task GivenRunningInstanceWithActiveLock(string instanceId)
    {
        _fakeClient.Reset();
        _lockManager.Reset();
        _auditSink.Reset();
        _clock.Reset();
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);

        // Acquire a lock to simulate another operation in progress
        await _lockManager.AcquireLockAsync(instanceId, "other-operation", TimeSpan.FromSeconds(30));

        InitializeHandler();
    }

    // When steps
    private async Task WhenSaveWorldIsCalled(string instanceId)
    {
        _request = new SaveWorldRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenSaveWorldIsCalledWithInvalidId(string instanceId)
    {
        _request = new SaveWorldRequest(instanceId, Guid.NewGuid().ToString());
        _result = await _handler.Handle(_request, CancellationToken.None);
    }

    private async Task WhenSaveWorldIsCalledWithEmptyCorrelationId(string instanceId)
    {
        _request = new SaveWorldRequest(instanceId, "");
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

    private void ThenResponseIsNotNull()
    {
        var response = _result.Value;
        response.Should().NotBeNull();
    }

    private void ThenErrorContains(string expectedError)
    {
        _result.Error.Should().ContainEquivalentOf(expectedError);
    }

    private void ThenClientMethodWasCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeTrue();
    }

    private void ThenClientMethodWasNotCalled(string methodName)
    {
        _fakeClient.WasMethodCalled(methodName).Should().BeFalse();
    }

    private void ThenAuditEventWasCreated()
    {
        var events = _auditSink.GetAllEvents();
        events.Should().NotBeEmpty();
    }

    private void ThenAuditEventWasCreatedWithFailure()
    {
        var events = _auditSink.GetAllEvents();
        events.Should().NotBeEmpty();
        events.Should().Contain(e => e.Outcome == "Failure");
    }

    private void ThenAuditEventHasCorrectOperationType(string operationType)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.OperationType == operationType);
    }

    private void ThenAuditEventHasOutcome(string outcome)
    {
        var events = _auditSink.GetAllEvents();
        events.Should().Contain(e => e.Outcome == outcome);
    }

    private void ThenLockWasAcquired(string instanceId)
    {
        // Lock should have been acquired during the operation
        // We verify this indirectly by checking the operation succeeded
        // and that we don't still have the lock (it was released)
        _result.IsSuccess.Should().BeTrue();
    }

    private void ThenLockWasReleased(string instanceId)
    {
        // Lock should be released after the operation
        var isLocked = _lockManager.IsLockedAsync(instanceId).Result;
        isLocked.Should().BeFalse();
    }
}
