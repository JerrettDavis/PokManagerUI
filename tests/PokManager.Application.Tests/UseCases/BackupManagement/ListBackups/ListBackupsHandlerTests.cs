using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.Tests.Fakes;
using Xunit;

namespace PokManager.Application.Tests.UseCases.BackupManagement.ListBackups;

/// <summary>
/// Tests for ListBackupsHandler using BDD-style naming conventions.
/// </summary>
public class ListBackupsHandlerTests
{
    private readonly InMemoryBackupStore _backupStore;
    private readonly ListBackupsHandler _handler;

    public ListBackupsHandlerTests()
    {
        _backupStore = new InMemoryBackupStore();
        _handler = new ListBackupsHandler(_backupStore);
    }

    [Fact]
    public async Task Given_ValidInstanceId_When_ListBackups_Then_ReturnsBackups()
    {
        // Arrange - Given an instance with existing backups
        var backup1 = new BackupInfo(
            BackupId: "backup-001",
            InstanceId: "island_main",
            Description: "First backup",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: 1024000,
            CreatedAt: DateTimeOffset.UtcNow.AddHours(-2),
            FilePath: "/backups/backup-001.gz",
            IsAutomatic: false,
            ServerVersion: "1.0.0"
        );

        var backup2 = new BackupInfo(
            BackupId: "backup-002",
            InstanceId: "island_main",
            Description: "Second backup",
            CompressionFormat: CompressionFormat.Zstd,
            SizeInBytes: 2048000,
            CreatedAt: DateTimeOffset.UtcNow.AddHours(-1),
            FilePath: "/backups/backup-002.zst",
            IsAutomatic: true,
            ServerVersion: "1.0.1"
        );

        _backupStore.AddBackup("island_main", backup1);
        _backupStore.AddBackup("island_main", backup2);

        var request = new ListBackupsRequest(
            InstanceId: "island_main",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should return success with backups sorted by date
        result.IsSuccess.Should().BeTrue();
        result.Value.Backups.Should().HaveCount(2);
        result.Value.Backups[0].BackupId.Should().Be("backup-002"); // Newest first
        result.Value.Backups[1].BackupId.Should().Be("backup-001");
    }

    [Fact]
    public async Task Given_NoBackups_When_ListBackups_Then_ReturnsEmptyList()
    {
        // Arrange - Given an instance with no backups
        var request = new ListBackupsRequest(
            InstanceId: "island_empty",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should return success with an empty list
        result.IsSuccess.Should().BeTrue();
        result.Value.Backups.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MultipleBackups_When_ListBackups_Then_ReturnsSortedByCreatedDateDescending()
    {
        // Arrange - Given an instance with multiple backups at different times
        var baseTime = DateTimeOffset.UtcNow;

        var backup1 = new BackupInfo(
            BackupId: "backup-oldest",
            InstanceId: "island_test",
            Description: "Oldest",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: 1000000,
            CreatedAt: baseTime.AddDays(-5),
            FilePath: "/backups/oldest.gz",
            IsAutomatic: false,
            ServerVersion: "1.0.0"
        );

        var backup2 = new BackupInfo(
            BackupId: "backup-middle",
            InstanceId: "island_test",
            Description: "Middle",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: 1100000,
            CreatedAt: baseTime.AddDays(-3),
            FilePath: "/backups/middle.gz",
            IsAutomatic: true,
            ServerVersion: "1.0.1"
        );

        var backup3 = new BackupInfo(
            BackupId: "backup-newest",
            InstanceId: "island_test",
            Description: "Newest",
            CompressionFormat: CompressionFormat.Zstd,
            SizeInBytes: 1200000,
            CreatedAt: baseTime.AddDays(-1),
            FilePath: "/backups/newest.zst",
            IsAutomatic: false,
            ServerVersion: "1.0.2"
        );

        // Add in random order to verify sorting
        _backupStore.AddBackup("island_test", backup2);
        _backupStore.AddBackup("island_test", backup1);
        _backupStore.AddBackup("island_test", backup3);

        var request = new ListBackupsRequest(
            InstanceId: "island_test",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should return backups sorted by created date descending
        result.IsSuccess.Should().BeTrue();
        result.Value.Backups.Should().HaveCount(3);
        result.Value.Backups[0].BackupId.Should().Be("backup-newest");
        result.Value.Backups[1].BackupId.Should().Be("backup-middle");
        result.Value.Backups[2].BackupId.Should().Be("backup-oldest");

        // Verify ordering is correct
        for (int i = 0; i < result.Value.Backups.Count - 1; i++)
        {
            result.Value.Backups[i].CreatedAt.Should().BeAfter(result.Value.Backups[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task Given_IncludeMetadataTrue_When_ListBackups_Then_IncludesFileSize()
    {
        // Arrange - Given an instance with backups and IncludeMetadata=true
        var backup = new BackupInfo(
            BackupId: "backup-001",
            InstanceId: "island_metadata",
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: 5242880, // 5 MB
            CreatedAt: DateTimeOffset.UtcNow,
            FilePath: "/backups/backup-001.gz",
            IsAutomatic: false,
            ServerVersion: "1.0.0"
        );

        _backupStore.AddBackup("island_metadata", backup);

        var request = new ListBackupsRequest(
            InstanceId: "island_metadata",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: true
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should include file size in the response
        result.IsSuccess.Should().BeTrue();
        result.Value.Backups.Should().HaveCount(1);
        result.Value.Backups[0].FileSizeBytes.Should().Be(5242880);
    }

    [Fact]
    public async Task Given_IncludeMetadataFalse_When_ListBackups_Then_ExcludesFileSize()
    {
        // Arrange - Given an instance with backups and IncludeMetadata=false
        var backup = new BackupInfo(
            BackupId: "backup-001",
            InstanceId: "island_no_metadata",
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: 5242880,
            CreatedAt: DateTimeOffset.UtcNow,
            FilePath: "/backups/backup-001.gz",
            IsAutomatic: false,
            ServerVersion: "1.0.0"
        );

        _backupStore.AddBackup("island_no_metadata", backup);

        var request = new ListBackupsRequest(
            InstanceId: "island_no_metadata",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should not include file size in the response
        result.IsSuccess.Should().BeTrue();
        result.Value.Backups.Should().HaveCount(1);
        result.Value.Backups[0].FileSizeBytes.Should().BeNull();
    }

    [Fact]
    public async Task Given_BackupStoreFails_When_ListBackups_Then_ReturnsFailure()
    {
        // Arrange - Given a backup store that will fail
        var failingStore = new FailingBackupStore();
        var handler = new ListBackupsHandler(failingStore);

        var request = new ListBackupsRequest(
            InstanceId: "island_fail",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: false
        );

        // Act - When handling the request
        var result = await handler.Handle(request, CancellationToken.None);

        // Assert - Then it should return a failure result
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Store failure");
    }

    [Fact]
    public async Task Given_ValidRequest_When_ListBackups_Then_MapsAllBackupPropertiesCorrectly()
    {
        // Arrange - Given an instance with a complete backup
        var createdAt = new DateTimeOffset(2024, 1, 19, 12, 0, 0, TimeSpan.Zero);
        var backup = new BackupInfo(
            BackupId: "backup-complete",
            InstanceId: "island_complete",
            Description: "Complete backup with all fields",
            CompressionFormat: CompressionFormat.Zstd,
            SizeInBytes: 9876543,
            CreatedAt: createdAt,
            FilePath: "/backups/complete.zst",
            IsAutomatic: true,
            ServerVersion: "2.1.5"
        );

        _backupStore.AddBackup("island_complete", backup);

        var request = new ListBackupsRequest(
            InstanceId: "island_complete",
            CorrelationId: Guid.NewGuid().ToString(),
            IncludeMetadata: true
        );

        // Act - When handling the request
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert - Then it should map all properties correctly
        result.IsSuccess.Should().BeTrue();
        var backupSummary = result.Value.Backups[0];

        backupSummary.BackupId.Should().Be("backup-complete");
        backupSummary.InstanceId.Should().Be("island_complete");
        backupSummary.CreatedAt.Should().Be(createdAt);
        backupSummary.CompressionFormat.Should().Be(CompressionFormat.Zstd);
        backupSummary.FileSizeBytes.Should().Be(9876543);
    }

    // Fake class for testing store failures
    private class FailingBackupStore : IBackupStore
    {
        public Task<PokManager.Domain.Common.Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
            string instanceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                PokManager.Domain.Common.Result.Failure<IReadOnlyList<BackupInfo>>("Store failure simulated for testing")
            );
        }

        public Task<PokManager.Domain.Common.Result<Stream>> GetBackupStreamAsync(
            string instanceId,
            string backupId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PokManager.Domain.Common.Result<PokManager.Domain.Common.Unit>> DeleteOldestBackupsAsync(
            string instanceId,
            int keepCount,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PokManager.Domain.Common.Result<long>> GetTotalBackupSizeAsync(
            string instanceId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PokManager.Domain.Common.Result<PokManager.Domain.Common.Unit>> StoreBackupAsync(
            BackupInfo backupInfo,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
