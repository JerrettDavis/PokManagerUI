using PokManager.Application.Models;
using PokManager.Domain.Enumerations;

namespace PokManager.Infrastructure.Tests.Fakes;

public class InMemoryBackupStoreTests
{
    private readonly InMemoryBackupStore _store = new();

    [Fact]
    public async Task ListBackupsAsync_WithNoBackups_ReturnsEmptyList()
    {
        // Act
        var result = await _store.ListBackupsAsync("instance-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ListBackupsAsync_WithBackups_ReturnsOrderedByCreatedAtDescending()
    {
        // Arrange
        var backup1 = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow.AddHours(-2));
        var backup2 = CreateBackupInfo("backup-2", "instance-1", DateTimeOffset.UtcNow.AddHours(-1));
        var backup3 = CreateBackupInfo("backup-3", "instance-1", DateTimeOffset.UtcNow);

        _store.AddBackup("instance-1", backup1);
        _store.AddBackup("instance-1", backup3);
        _store.AddBackup("instance-1", backup2);

        // Act
        var result = await _store.ListBackupsAsync("instance-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.Equal("backup-3", result.Value[0].BackupId);
        Assert.Equal("backup-2", result.Value[1].BackupId);
        Assert.Equal("backup-1", result.Value[2].BackupId);
    }

    [Fact]
    public async Task GetBackupStreamAsync_WithNonExistentInstance_ReturnsFailure()
    {
        // Act
        var result = await _store.GetBackupStreamAsync("non-existent", "backup-1");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("InstanceNotFound", result.Error);
    }

    [Fact]
    public async Task GetBackupStreamAsync_WithNonExistentBackup_ReturnsFailure()
    {
        // Arrange
        var backup = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow);
        _store.AddBackup("instance-1", backup);

        // Act
        var result = await _store.GetBackupStreamAsync("instance-1", "non-existent");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("BackupNotFound", result.Error);
    }

    [Fact]
    public async Task GetBackupStreamAsync_WithExistingBackup_ReturnsStream()
    {
        // Arrange
        var backup = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow);
        var data = new byte[] { 1, 2, 3, 4, 5 };
        _store.AddBackup("instance-1", backup, data);

        // Act
        var result = await _store.GetBackupStreamAsync("instance-1", "backup-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using var stream = result.Value;
        var buffer = new byte[data.Length];
        var bytesRead = await stream.ReadAsync(buffer);
        Assert.Equal(data.Length, bytesRead);
        Assert.Equal(data, buffer);
    }

    [Fact]
    public async Task DeleteOldestBackupsAsync_WithNoInstance_ReturnsSuccess()
    {
        // Act
        var result = await _store.DeleteOldestBackupsAsync("non-existent", 3);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteOldestBackupsAsync_WithFewerBackupsThanKeepCount_DoesNotDelete()
    {
        // Arrange
        var backup1 = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow.AddHours(-2));
        var backup2 = CreateBackupInfo("backup-2", "instance-1", DateTimeOffset.UtcNow.AddHours(-1));
        _store.AddBackup("instance-1", backup1);
        _store.AddBackup("instance-1", backup2);

        // Act
        var result = await _store.DeleteOldestBackupsAsync("instance-1", 5);

        // Assert
        Assert.True(result.IsSuccess);
        var backups = await _store.ListBackupsAsync("instance-1");
        Assert.Equal(2, backups.Value.Count);
    }

    [Fact]
    public async Task DeleteOldestBackupsAsync_WithMoreBackupsThanKeepCount_DeletesOldest()
    {
        // Arrange
        var backup1 = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow.AddHours(-3));
        var backup2 = CreateBackupInfo("backup-2", "instance-1", DateTimeOffset.UtcNow.AddHours(-2));
        var backup3 = CreateBackupInfo("backup-3", "instance-1", DateTimeOffset.UtcNow.AddHours(-1));
        var backup4 = CreateBackupInfo("backup-4", "instance-1", DateTimeOffset.UtcNow);

        _store.AddBackup("instance-1", backup1);
        _store.AddBackup("instance-1", backup2);
        _store.AddBackup("instance-1", backup3);
        _store.AddBackup("instance-1", backup4);

        // Act
        var result = await _store.DeleteOldestBackupsAsync("instance-1", 2);

        // Assert
        Assert.True(result.IsSuccess);
        var backups = await _store.ListBackupsAsync("instance-1");
        Assert.Equal(2, backups.Value.Count);
        Assert.Equal("backup-4", backups.Value[0].BackupId);
        Assert.Equal("backup-3", backups.Value[1].BackupId);

        // Verify oldest backup data is also deleted
        var oldBackup = await _store.GetBackupStreamAsync("instance-1", "backup-1");
        Assert.True(oldBackup.IsFailure);
    }

    [Fact]
    public async Task GetTotalBackupSizeAsync_WithNoBackups_ReturnsZero()
    {
        // Act
        var result = await _store.GetTotalBackupSizeAsync("instance-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public async Task GetTotalBackupSizeAsync_WithMultipleBackups_ReturnsSumOfSizes()
    {
        // Arrange
        var backup1 = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow, sizeBytes: 1000);
        var backup2 = CreateBackupInfo("backup-2", "instance-1", DateTimeOffset.UtcNow, sizeBytes: 2000);
        var backup3 = CreateBackupInfo("backup-3", "instance-1", DateTimeOffset.UtcNow, sizeBytes: 3000);

        _store.AddBackup("instance-1", backup1);
        _store.AddBackup("instance-1", backup2);
        _store.AddBackup("instance-1", backup3);

        // Act
        var result = await _store.GetTotalBackupSizeAsync("instance-1");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(6000, result.Value);
    }

    [Fact]
    public async Task Reset_ClearsAllBackups()
    {
        // Arrange
        var backup1 = CreateBackupInfo("backup-1", "instance-1", DateTimeOffset.UtcNow);
        var backup2 = CreateBackupInfo("backup-2", "instance-2", DateTimeOffset.UtcNow);
        _store.AddBackup("instance-1", backup1);
        _store.AddBackup("instance-2", backup2);

        // Act
        _store.Reset();

        // Assert
        var result1 = await _store.ListBackupsAsync("instance-1");
        var result2 = await _store.ListBackupsAsync("instance-2");
        Assert.Empty(result1.Value);
        Assert.Empty(result2.Value);
    }

    [Fact]
    public async Task Store_IsThreadSafe_WhenAddingBackupsConcurrently()
    {
        // Arrange
        const int taskCount = 10;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var backupId = $"backup-{i}";
            tasks.Add(Task.Run(() =>
            {
                var backup = CreateBackupInfo(backupId, "instance-1", DateTimeOffset.UtcNow);
                _store.AddBackup("instance-1", backup);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var result = await _store.ListBackupsAsync("instance-1");
        Assert.Equal(taskCount, result.Value.Count);
    }

    private static BackupInfo CreateBackupInfo(
        string backupId,
        string instanceId,
        DateTimeOffset createdAt,
        long sizeBytes = 1024)
    {
        return new BackupInfo(
            BackupId: backupId,
            InstanceId: instanceId,
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Gzip,
            SizeInBytes: sizeBytes,
            CreatedAt: createdAt,
            FilePath: $"/backups/{backupId}.tar.gz",
            IsAutomatic: false,
            ServerVersion: "1.0.0"
        );
    }
}
