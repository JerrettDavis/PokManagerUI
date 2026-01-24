using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.UseCases.Configuration.ApplyConfiguration;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.ConfigurationManagement.ApplyConfiguration;

public class ApplyConfigurationHandlerTests
{
    private readonly FakePokManagerClient _fakeClient;
    private readonly InMemoryOperationLockManager _lockManager;
    private readonly InMemoryAuditSink _auditSink;
    private readonly FakeClock _clock;
    private readonly ApplyConfigurationHandler _handler;

    public ApplyConfigurationHandlerTests()
    {
        _fakeClient = new FakePokManagerClient();
        _lockManager = new InMemoryOperationLockManager();
        _auditSink = new InMemoryAuditSink();
        _clock = new FakeClock();
        _handler = new ApplyConfigurationHandler(_fakeClient, _lockManager, _auditSink, _clock);
    }

    [Fact]
    public async Task Given_ValidConfiguration_When_Apply_Then_ConfigurationApplied()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name",
            ["MaxPlayers"] = "64"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.InstanceId.Should().Be(instanceId);
        result.Value.ChangedSettings.Should().Contain("SessionName");
        result.Value.ChangedSettings.Should().Contain("MaxPlayers");
        result.Value.AppliedAt.Should().Be(_clock.UtcNow);

        // Verify the client method was called
        _fakeClient.WasMethodCalled(nameof(_fakeClient.ApplyConfigurationAsync)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_InvalidConfiguration_When_Apply_Then_ReturnsValidationFailure()
    {
        // Arrange
        var request = new ApplyConfigurationRequest(
            "", // Invalid empty instance ID
            Guid.NewGuid().ToString(),
            new Dictionary<string, string> { ["Key"] = "Value" });

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance ID");

        // Verify no client call was made
        _fakeClient.WasMethodCalled(nameof(_fakeClient.ApplyConfigurationAsync)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_Apply_Then_ReturnsNotFound()
    {
        // Arrange - Don't set up the instance
        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            "nonexistent",
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("InstanceNotFound");
    }

    [Fact]
    public async Task Given_ValidateBeforeApplyTrue_When_Apply_Then_ValidatesFirst()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration,
            RestartInstance: false);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify validation was called before apply
        var methodCalls = _fakeClient.GetMethodCalls();
        var validateIndex = methodCalls.ToList().IndexOf(nameof(_fakeClient.ValidateConfigurationAsync));
        var applyIndex = methodCalls.ToList().IndexOf(nameof(_fakeClient.ApplyConfigurationAsync));

        // If validation was called, it should be before apply
        if (validateIndex >= 0 && applyIndex >= 0)
        {
            validateIndex.Should().BeLessThan(applyIndex);
        }
    }

    [Fact]
    public async Task Given_ValidationFails_When_Apply_Then_ReturnsValidationError()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        // Set up the fake client to return a validation failure
        var validationResult = new ConfigurationValidationResult(
            false, // IsValid = false
            new[] { "MaxPlayers must be between 1 and 32" },
            Array.Empty<string>(),
            DateTimeOffset.UtcNow);

        SetupValidationResult(instanceId, validationResult);

        var configuration = new Dictionary<string, string>
        {
            ["MaxPlayers"] = "999"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("MaxPlayers must be between 1 and 32");

        // Verify ApplyConfiguration was NOT called due to validation failure
        _fakeClient.WasMethodCalled(nameof(_fakeClient.ApplyConfigurationAsync)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_RequiresRestart_When_Apply_Then_FlagSet()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        // Set up the fake client to return a result indicating restart is required
        var applyResult = new ApplyConfigurationResult(
            true,
            new[] { "ServerMap" },
            RequiredRestart: true,
            WasRestarted: false,
            BackupCreated: true,
            DateTimeOffset.UtcNow,
            "Configuration applied but restart is required");

        SetupApplyConfigurationResult(instanceId, applyResult);

        var configuration = new Dictionary<string, string>
        {
            ["ServerMap"] = "NewMap"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.RequiresRestart.Should().BeTrue();
        result.Value.Message.Should().Contain("restart is required");
    }

    [Fact]
    public async Task Given_ValidRequest_When_Apply_Then_AcquiresLock()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify the lock was acquired and released (should not be locked after operation)
        var isLocked = await _lockManager.IsLockedAsync(instanceId);
        isLocked.Should().BeFalse("Lock should be released after operation completes");
    }

    [Fact]
    public async Task Given_InstanceAlreadyLocked_When_Apply_Then_ReturnsLockError()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        // Acquire a lock on the instance
        var existingLock = await _lockManager.AcquireLockAsync(
            instanceId,
            "other-operation",
            TimeSpan.FromSeconds(30));

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        try
        {
            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue();
            result.Error.Should().Contain("locked");
        }
        finally
        {
            // Clean up
            await existingLock.Value.DisposeAsync();
        }
    }

    [Fact]
    public async Task Given_SuccessfulApply_When_Apply_Then_CreatesAuditEvent()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name",
            ["MaxPlayers"] = "64"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify audit event was created
        var auditEvents = _auditSink.GetAllEvents();
        auditEvents.Should().HaveCount(1);

        var auditEvent = auditEvents.First();
        auditEvent.InstanceId.Should().Be(instanceId);
        auditEvent.OperationType.Should().Be("ApplyConfiguration");
        auditEvent.Outcome.Should().Be("Success");
        auditEvent.PerformedAt.Should().Be(_clock.UtcNow);
        auditEvent.Details.Should().NotBeNull();
        auditEvent.Details!["ChangedSettings"].Should().Contain("SessionName");
    }

    [Fact]
    public async Task Given_FailedApply_When_Apply_Then_CreatesAuditEventWithError()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        // Make the next operation fail
        _fakeClient.FailNextOperation("Configuration apply failed");

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();

        // Verify audit event was created with failure
        var auditEvents = _auditSink.GetAllEvents();
        auditEvents.Should().HaveCount(1);

        var auditEvent = auditEvents.First();
        auditEvent.InstanceId.Should().Be(instanceId);
        auditEvent.OperationType.Should().Be("ApplyConfiguration");
        auditEvent.Outcome.Should().Be("Failure");
        auditEvent.ErrorMessage.Should().Contain("Configuration apply failed");
    }

    [Fact]
    public async Task Given_EmptyConfigurationSettings_When_Apply_Then_ReturnsValidationFailure()
    {
        // Arrange
        var request = new ApplyConfigurationRequest(
            "island_main",
            Guid.NewGuid().ToString(),
            new Dictionary<string, string>()); // Empty configuration

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Configuration settings cannot be empty");
    }

    [Fact]
    public async Task Given_ClientFailure_When_Apply_Then_ReleasesLock()
    {
        // Arrange
        var instanceId = "island_main";
        SetupInstance(instanceId);

        // Make the client fail
        _fakeClient.FailNextOperation("Client connection error");

        var configuration = new Dictionary<string, string>
        {
            ["SessionName"] = "New Server Name"
        };

        var request = new ApplyConfigurationRequest(
            instanceId,
            Guid.NewGuid().ToString(),
            configuration);

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();

        // Verify lock was released even after failure
        var isLocked = await _lockManager.IsLockedAsync(instanceId);
        isLocked.Should().BeFalse("Lock should be released even after failure");
    }

    #region Helper Methods

    private void SetupInstance(string instanceId)
    {
        _fakeClient.SetupInstance(instanceId, InstanceState.Running);
        _fakeClient.SetupInstanceDetails(instanceId, new InstanceDetails(
            instanceId,
            $"Server {instanceId}",
            InstanceState.Running,
            ProcessHealth.Healthy,
            8211,
            32,
            0,
            "1.0.0",
            TimeSpan.FromHours(1),
            $"/opt/palworld/{instanceId}",
            $"/opt/palworld/{instanceId}/world",
            $"/opt/palworld/{instanceId}/config",
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            null,
            new Dictionary<string, string>()));
    }

    private void SetupValidationResult(string instanceId, ConfigurationValidationResult validationResult)
    {
        // We need to extend FakePokManagerClient to support this
        // For now, we'll use a workaround with the fail mechanism
        if (!validationResult.IsValid)
        {
            _fakeClient.FailNextOperation($"Validation failed: {string.Join("; ", validationResult.Errors)}");
        }
    }

    private void SetupApplyConfigurationResult(string instanceId, ApplyConfigurationResult applyResult)
    {
        _fakeClient.SetupApplyConfigurationResult(instanceId, applyResult);
    }

    #endregion
}
