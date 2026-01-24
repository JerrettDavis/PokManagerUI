using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Enumerations;
using PokManager.Infrastructure.PokManager;
using PokManager.Infrastructure.Shell;
using TinyBDD;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager;

/// <summary>
/// Tests for PokManagerClient backup operations following TDD principles.
/// Uses mocked IBashCommandExecutor to test the client backup management behavior.
/// </summary>
public class PokManagerClientBackupTests
{
    private readonly IBashCommandExecutor _mockExecutor;
    private readonly PokManagerClientConfiguration _configuration;
    private readonly IPokManagerClient _client;

    public PokManagerClientBackupTests()
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

    #region ListBackupsAsync Tests

    [Fact]
    public async Task ListBackupsAsync_ShouldReturnBackupList_WhenBackupsExist()
    {
        // Given an instance with multiple backups
        var backupOutput = @"backup_server1_20250119_143022.tar.gz
backup_server1_20250118_120000.tar.zst
backup_server1_20250117_090000.tar.gz";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, backupOutput, ""));

        // When listing backups
        var result = await _client.ListBackupsAsync("server1", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].InstanceId.Should().Be("server1");
        result.Value[0].CompressionFormat.Should().Be(CompressionFormat.Gzip);
    }

    [Fact]
    public async Task ListBackupsAsync_ShouldReturnEmptyList_WhenNoBackupsExist()
    {
        // Given an instance with no backups
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, "", ""));

        // When listing backups
        var result = await _client.ListBackupsAsync("server1", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task ListBackupsAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Instance not found"));

        // When listing backups
        var result = await _client.ListBackupsAsync("nonexistent", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to list backups");
    }

    [Fact]
    public async Task ListBackupsAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When listing backups
        var act = async () => await _client.ListBackupsAsync(instanceId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region CreateBackupAsync Tests

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateBackup_WithDefaultOptions()
    {
        // Given an instance without specific backup options
        var backupOutput = "Backup created successfully: backup_server1_20250119_143022.tar.gz";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, backupOutput, ""));

        // When creating a backup with default options
        var result = await _client.CreateBackupAsync("server1", null, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain("backup_server1_");
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldCreateBackup_WithCustomOptions()
    {
        // Given an instance with custom backup options
        var backupOutput = "Backup created successfully: backup_server1_20250119_143022.tar.zst";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, backupOutput, ""));

        var options = new CreateBackupOptions(
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Zstd,
            IncludeConfiguration: true,
            IncludeLogs: false
        );

        // When creating a backup with custom options
        var result = await _client.CreateBackupAsync("server1", options, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain(".tar.zst");
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Failed to create backup: disk full"));

        // When creating a backup
        var result = await _client.CreateBackupAsync("server1", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to create backup");
    }

    [Fact]
    public async Task CreateBackupAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When creating a backup
        var act = async () => await _client.CreateBackupAsync(instanceId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region RestoreBackupAsync Tests

    [Fact]
    public async Task RestoreBackupAsync_ShouldRestoreBackup_WithDefaultOptions()
    {
        // Given a valid backup to restore
        var restoreOutput = "Backup restored successfully";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, restoreOutput, ""));

        // When restoring a backup with default options
        var result = await _client.RestoreBackupAsync("server1", "backup_server1_20250119_143022.tar.gz", null, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldRestoreBackup_WithCustomOptions()
    {
        // Given a valid backup with custom restore options
        var restoreOutput = "Instance stopped\nBackup restored successfully\nInstance started";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, restoreOutput, ""));

        var options = new RestoreBackupOptions(
            StopInstance: true,
            StartAfterRestore: true,
            BackupBeforeRestore: true,
            ValidateBackup: true
        );

        // When restoring a backup with custom options
        var result = await _client.RestoreBackupAsync("server1", "backup_server1_20250119_143022.tar.gz", options, CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Backup file not found"));

        // When restoring a backup
        var result = await _client.RestoreBackupAsync("server1", "invalid_backup", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to restore backup");
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When restoring a backup
        var act = async () => await _client.RestoreBackupAsync(instanceId, "backup_id", null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RestoreBackupAsync_ShouldThrowArgumentException_WhenBackupIdIsEmpty()
    {
        // Given an empty backup ID
        var backupId = "";

        // When restoring a backup
        var act = async () => await _client.RestoreBackupAsync("server1", backupId, null, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DownloadBackupAsync Tests

    [Fact(Skip = "Requires actual file system - implementation opens FileStream which can't be mocked with current approach")]
    public async Task DownloadBackupAsync_ShouldReturnStream_WhenBackupExists()
    {
        // Given a valid backup file
        var backupPath = Path.Combine(_configuration.InstancesBasePath, "backups", "backup_server1_20250119_143022.tar.gz");

        // Create a mock file stream
        var mockFileContent = new byte[] { 0x1F, 0x8B, 0x08, 0x00 }; // Gzip magic bytes

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, backupPath, ""));

        // When downloading a backup
        var result = await _client.DownloadBackupAsync("server1", "backup_server1_20250119_143022.tar.gz", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task DownloadBackupAsync_ShouldReturnFailure_WhenBackupNotFound()
    {
        // Given a non-existent backup
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Backup file not found"));

        // When downloading a backup
        var result = await _client.DownloadBackupAsync("server1", "nonexistent_backup", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to download backup");
    }

    [Fact]
    public async Task DownloadBackupAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When downloading a backup
        var act = async () => await _client.DownloadBackupAsync(instanceId, "backup_id", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DownloadBackupAsync_ShouldThrowArgumentException_WhenBackupIdIsEmpty()
    {
        // Given an empty backup ID
        var backupId = "";

        // When downloading a backup
        var act = async () => await _client.DownloadBackupAsync("server1", backupId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region DeleteBackupAsync Tests

    [Fact]
    public async Task DeleteBackupAsync_ShouldDeleteBackup_WhenBackupExists()
    {
        // Given a valid backup to delete
        var deleteOutput = "Backup deleted successfully: backup_server1_20250119_143022.tar.gz";

        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(0, deleteOutput, ""));

        // When deleting a backup
        var result = await _client.DeleteBackupAsync("server1", "backup_server1_20250119_143022.tar.gz", CancellationToken.None);

        // Then the result should be successful
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBackupAsync_ShouldReturnFailure_WhenCommandFails()
    {
        // Given a failed command execution
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Returns(new BashCommandResult(1, "", "Backup not found"));

        // When deleting a backup
        var result = await _client.DeleteBackupAsync("server1", "nonexistent_backup", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Failed to delete backup");
    }

    [Fact]
    public async Task DeleteBackupAsync_ShouldThrowArgumentException_WhenInstanceIdIsEmpty()
    {
        // Given an empty instance ID
        var instanceId = "";

        // When deleting a backup
        var act = async () => await _client.DeleteBackupAsync(instanceId, "backup_id", CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task DeleteBackupAsync_ShouldThrowArgumentException_WhenBackupIdIsEmpty()
    {
        // Given an empty backup ID
        var backupId = "";

        // When deleting a backup
        var act = async () => await _client.DeleteBackupAsync("server1", backupId, CancellationToken.None);

        // Then an ArgumentException should be thrown
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task BackupOperations_ShouldHandleTimeoutGracefully()
    {
        // Given a command that times out
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new TimeoutException("Command timed out"));

        // When listing backups
        var result = await _client.ListBackupsAsync("server1", CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task BackupOperations_ShouldHandleCancellationGracefully()
    {
        // Given a cancelled operation
        _mockExecutor
            .ExecuteAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<TimeSpan>(),
                Arg.Any<CancellationToken>())
            .Throws(new OperationCanceledException());

        // When creating a backup
        var result = await _client.CreateBackupAsync("server1", null, CancellationToken.None);

        // Then the result should be a failure
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cancel");
    }

    #endregion
}
