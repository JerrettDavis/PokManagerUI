using FluentAssertions;
using PokManager.Domain.ValueObjects;
using PokManager.Infrastructure.PokManager.Commands;
using Xunit;

namespace PokManager.Infrastructure.Tests.PokManager.Commands;

public class RestoreCommandBuilderTests
{
    private const string DefaultScriptPath = "/usr/local/bin/pok.sh";

    [Fact]
    public void Build_WithInstanceIdAndBackupId_ShouldCreateRestoreCommand()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .FromBackup(backupId)
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00");
    }

    [Fact]
    public void Build_WithoutInstanceId_ShouldReturnFailure()
    {
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .FromBackup(backupId)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Instance");
    }

    [Fact]
    public void Build_WithoutBackupId_ShouldReturnFailure()
    {
        var instanceId = InstanceId.Create("island_main").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .Build();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Backup");
    }

    [Fact]
    public void Build_WithForceFlag_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .FromBackup(backupId)
            .WithForce()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00 --force");
    }

    [Fact]
    public void Build_WithStopBeforeRestore_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .FromBackup(backupId)
            .WithStopBeforeRestore()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00 --stop-before-restore");
    }

    [Fact]
    public void Build_WithStartAfterRestore_ShouldIncludeFlag()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .FromBackup(backupId)
            .WithStartAfterRestore()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00 --start-after-restore");
    }

    [Fact]
    public void Build_WithMultipleOptions_ShouldIncludeAll()
    {
        var instanceId = InstanceId.Create("island_main").Value;
        var backupId = BackupId.Create("island_main_backup_2025-01-19_12-00-00").Value;

        var result = RestoreCommandBuilder
            .Create(DefaultScriptPath)
            .ForInstance(instanceId)
            .FromBackup(backupId)
            .WithStopBeforeRestore()
            .WithStartAfterRestore()
            .WithForce()
            .Build();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("/usr/local/bin/pok.sh restore island_main --backup-id island_main_backup_2025-01-19_12-00-00 --stop-before-restore --start-after-restore --force");
    }
}
