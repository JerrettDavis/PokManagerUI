using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Domain.Common;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.BackupManagement.CreateBackup;

/// <summary>
/// Tests for CreateBackupHandler using BDD-style naming conventions.
/// </summary>
public class CreateBackupHandlerTests
{
    private readonly FakePokManagerClient _pokManagerClient;
    private readonly InMemoryBackupStore _backupStore;
    private readonly InMemoryOperationLockManager _lockManager;
    private readonly InMemoryAuditSink _auditSink;
    private readonly FakeClock _clock;
    private readonly CreateBackupHandler _handler;

    public CreateBackupHandlerTests()
    {
        _pokManagerClient = new FakePokManagerClient();
        _backupStore = new InMemoryBackupStore();
        _lockManager = new InMemoryOperationLockManager();
        _auditSink = new InMemoryAuditSink();
        _clock = new FakeClock();

        _handler = new CreateBackupHandler(
            _pokManagerClient,
            _backupStore,
            _lockManager,
            _auditSink,
            _clock
        );
    }

    [Fact]
    public async Task Given_ValidInstance_When_CreateBackup_Then_BackupCreated()
    {
        // Arrange - Given a valid running instance
        var instanceId = "island_main";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the backup is created successfully
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.BackupId.Should().NotBeEmpty();
        result.Value.InstanceId.Should().Be(instanceId);
        result.Value.Success.Should().BeTrue();

        // Verify PokManagerClient was called
        _pokManagerClient.WasMethodCalled(nameof(IPokManagerClient.CreateBackupAsync)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_InvalidInstanceId_When_CreateBackup_Then_ReturnsValidationFailure()
    {
        // Arrange - Given an invalid instance ID
        var request = new CreateBackupRequest(
            InstanceId: "", // Empty instance ID
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it returns a validation failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance ID");
    }

    [Fact]
    public async Task Given_InstanceNotFound_When_CreateBackup_Then_ReturnsNotFound()
    {
        // Arrange - Given an instance that doesn't exist
        var request = new CreateBackupRequest(
            InstanceId: "nonexistent_instance",
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it returns an instance not found error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("InstanceNotFound");
    }

    [Fact]
    public async Task Given_WithCompression_When_CreateBackup_Then_UsesCompression()
    {
        // Arrange - Given a valid instance and compression options
        var instanceId = "island_compressed";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var options = new CreateBackupOptions(
            Description: "Compressed backup",
            CompressionFormat: CompressionFormat.Zstd,
            IncludeConfiguration: true,
            IncludeLogs: false
        );

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: options
        );

        // Act - When creating a backup with compression
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the backup uses the specified compression format
        result.IsSuccess.Should().BeTrue();

        var backups = await _backupStore.ListBackupsAsync(instanceId);
        backups.IsSuccess.Should().BeTrue();
        backups.Value.Should().HaveCount(1);
        backups.Value[0].CompressionFormat.Should().Be(CompressionFormat.Zstd);
    }

    [Fact]
    public async Task Given_WithDescription_When_CreateBackup_Then_StoresDescription()
    {
        // Arrange - Given a valid instance and a description
        var instanceId = "island_described";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var description = "Pre-update backup before server update";
        var options = new CreateBackupOptions(
            Description: description,
            CompressionFormat: CompressionFormat.Gzip
        );

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: options
        );

        // Act - When creating a backup with a description
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the backup stores the description
        result.IsSuccess.Should().BeTrue();

        var backups = await _backupStore.ListBackupsAsync(instanceId);
        backups.IsSuccess.Should().BeTrue();
        backups.Value.Should().HaveCount(1);
        backups.Value[0].Description.Should().Be(description);
    }

    [Fact]
    public async Task Given_WithIncludeConfiguration_When_CreateBackup_Then_IncludesConfiguration()
    {
        // Arrange - Given a valid instance and configuration options
        var instanceId = "island_config";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var options = new CreateBackupOptions(
            Description: "Backup with config",
            CompressionFormat: CompressionFormat.Gzip,
            IncludeConfiguration: true,
            IncludeLogs: false
        );

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: options
        );

        // Act - When creating a backup with configuration included
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the backup is created successfully with configuration
        result.IsSuccess.Should().BeTrue();
        _pokManagerClient.WasMethodCalled(nameof(IPokManagerClient.CreateBackupAsync)).Should().BeTrue();
    }

    [Fact]
    public async Task Given_BackupInProgress_When_CreateBackup_Then_ReturnsError()
    {
        // Arrange - Given an instance with an operation in progress
        var instanceId = "island_locked";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        // Acquire a lock manually to simulate an operation in progress
        var existingLock = await _lockManager.AcquireLockAsync(
            instanceId,
            "other_operation",
            TimeSpan.FromMinutes(1)
        );
        existingLock.IsSuccess.Should().BeTrue();

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When attempting to create a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it returns a lock error
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("locked");

        // Clean up
        await existingLock.Value.DisposeAsync();
    }

    [Fact]
    public async Task Given_SuccessfulBackup_When_CreateBackup_Then_CreatesAuditEvent()
    {
        // Arrange - Given a valid instance
        var instanceId = "island_audit";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then an audit event is created
        result.IsSuccess.Should().BeTrue();

        var auditEvents = _auditSink.GetAllEvents();
        auditEvents.Should().HaveCount(1);
        auditEvents[0].InstanceId.Should().Be(instanceId);
        auditEvents[0].OperationType.Should().Be("CreateBackup");
        auditEvents[0].Outcome.Should().Be("Success");
    }

    [Fact]
    public async Task Given_BackupCreated_When_CreateBackup_Then_StoresInBackupStore()
    {
        // Arrange - Given a valid instance
        var instanceId = "island_store";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var options = new CreateBackupOptions(
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Gzip
        );

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: options
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the backup is stored in the backup store
        result.IsSuccess.Should().BeTrue();

        var backups = await _backupStore.ListBackupsAsync(instanceId);
        backups.IsSuccess.Should().BeTrue();
        backups.Value.Should().HaveCount(1);
        backups.Value[0].BackupId.Should().Be(result.Value.BackupId);
        backups.Value[0].InstanceId.Should().Be(instanceId);
        backups.Value[0].Description.Should().Be("Test backup");
        backups.Value[0].IsAutomatic.Should().BeFalse();
    }

    [Fact]
    public async Task Given_PokManagerClientFails_When_CreateBackup_Then_ReturnsFailureAndCreatesAuditEvent()
    {
        // Arrange - Given a valid instance that will fail backup creation
        var instanceId = "island_fail";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);
        _pokManagerClient.FailNextOperation("Backup creation failed");

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup that fails
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it returns a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Backup creation failed");

        // Verify audit event was created for the failure
        var auditEvents = _auditSink.GetAllEvents();
        auditEvents.Should().HaveCount(1);
        auditEvents[0].Outcome.Should().Be("Failure");
        auditEvents[0].ErrorMessage.Should().Contain("Backup creation failed");
    }

    [Fact]
    public async Task Given_SuccessfulBackup_When_CreateBackup_Then_ReleasesLock()
    {
        // Arrange - Given a valid instance
        var instanceId = "island_lock_release";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the lock is released
        result.IsSuccess.Should().BeTrue();

        var isLocked = await _lockManager.IsLockedAsync(instanceId);
        isLocked.Should().BeFalse();
    }

    [Fact]
    public async Task Given_FailedBackup_When_CreateBackup_Then_ReleasesLock()
    {
        // Arrange - Given an instance that will fail backup
        var instanceId = "island_fail_lock";
        _pokManagerClient.SetupInstance(instanceId, InstanceState.Running);
        _pokManagerClient.FailNextOperation("Test failure");

        var request = new CreateBackupRequest(
            InstanceId: instanceId,
            CorrelationId: Guid.NewGuid().ToString(),
            Options: null
        );

        // Act - When creating a backup that fails
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then the lock is still released
        result.IsFailure.Should().BeTrue();

        var isLocked = await _lockManager.IsLockedAsync(instanceId);
        isLocked.Should().BeFalse();
    }
}
