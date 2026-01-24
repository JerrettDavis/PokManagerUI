using FluentAssertions;
using PokManager.Domain.Entities;
using PokManager.Domain.Enumerations;
using Xunit;

namespace PokManager.Domain.Tests.Entities;

public class BackupTests
{
    [Fact]
    public void Backup_Can_Be_Created_With_Valid_Data()
    {
        var backup = new Backup(
            "backup_123",
            "instance_456",
            1024 * 1024 * 500, // 500 MB
            CompressionFormat.Gzip,
            "/backups/backup_123.tar.gz");

        backup.BackupId.Should().Be("backup_123");
        backup.InstanceId.Should().Be("instance_456");
        backup.SizeBytes.Should().Be(1024 * 1024 * 500);
        backup.CompressionFormat.Should().Be(CompressionFormat.Gzip);
        backup.FilePath.Should().Be("/backups/backup_123.tar.gz");
        backup.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Backup_Cannot_Be_Created_With_Zero_Size()
    {
        var action = () => new Backup(
            "backup_123",
            "instance_456",
            0,
            CompressionFormat.Gzip,
            "/backups/backup_123.zip");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*SizeBytes must be greater than 0*");
    }

    [Fact]
    public void Backup_Cannot_Be_Created_With_Negative_Size()
    {
        var action = () => new Backup(
            "backup_123",
            "instance_456",
            -100,
            CompressionFormat.Gzip,
            "/backups/backup_123.zip");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*SizeBytes must be greater than 0*");
    }

    [Fact]
    public void Backup_Cannot_Be_Created_With_Empty_FilePath()
    {
        var action = () => new Backup(
            "backup_123",
            "instance_456",
            1024,
            CompressionFormat.Gzip,
            "");

        action.Should().Throw<ArgumentException>()
            .WithMessage("*FilePath cannot be empty*");
    }

    [Fact]
    public void Backup_Cannot_Be_Created_With_Null_FilePath()
    {
        var action = () => new Backup(
            "backup_123",
            "instance_456",
            1024,
            CompressionFormat.Gzip,
            null!);

        action.Should().Throw<ArgumentException>()
            .WithMessage("*FilePath cannot be empty*");
    }

    [Fact]
    public void Backup_Properties_Are_Immutable()
    {
        var backup = new Backup(
            "backup_123",
            "instance_456",
            1024,
            CompressionFormat.Gzip,
            "/backups/backup_123.zip");

        // Verify all properties have init-only setters or no setters
        var backupIdProperty = typeof(Backup).GetProperty(nameof(Backup.BackupId));
        var instanceIdProperty = typeof(Backup).GetProperty(nameof(Backup.InstanceId));
        var sizeProperty = typeof(Backup).GetProperty(nameof(Backup.SizeBytes));
        var formatProperty = typeof(Backup).GetProperty(nameof(Backup.CompressionFormat));
        var pathProperty = typeof(Backup).GetProperty(nameof(Backup.FilePath));
        var createdAtProperty = typeof(Backup).GetProperty(nameof(Backup.CreatedAt));

        backupIdProperty!.CanWrite.Should().BeFalse();
        instanceIdProperty!.CanWrite.Should().BeFalse();
        sizeProperty!.CanWrite.Should().BeFalse();
        formatProperty!.CanWrite.Should().BeFalse();
        pathProperty!.CanWrite.Should().BeFalse();
        createdAtProperty!.CanWrite.Should().BeFalse();
    }
}
