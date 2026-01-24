using FluentAssertions;
using PokManager.Application.Models;
using PokManager.Domain.Enumerations;
using Xunit;

namespace PokManager.Infrastructure.Tests.Fakes;

public class FakePokManagerClientTests
{
    [Fact]
    public async Task FakePokManagerClient_Can_Be_Setup_With_Instance_State()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var result = await fake.GetInstanceStatusAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.InstanceId.Should().Be("island_main");
        result.Value.State.Should().Be(InstanceState.Running);
    }

    [Fact]
    public async Task FakePokManagerClient_Can_Simulate_Failure()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.FailNextOperation("Simulated error");

        // Act
        var result = await fake.ListInstancesAsync();

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Simulated error");
    }

    [Fact]
    public async Task FakePokManagerClient_Records_Method_Calls()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Stopped);

        // Act
        await fake.StartInstanceAsync("island_main");

        // Assert
        fake.WasMethodCalled(nameof(FakePokManagerClient.StartInstanceAsync)).Should().BeTrue();
        fake.GetMethodCallCount(nameof(FakePokManagerClient.StartInstanceAsync)).Should().Be(1);
    }

    [Fact]
    public async Task FakePokManagerClient_Can_Simulate_Delay()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        fake.SimulateDelay(TimeSpan.FromMilliseconds(100));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await fake.GetInstanceStatusAsync("island_main");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(90); // Allow some margin
    }

    [Fact]
    public async Task FakePokManagerClient_Reset_Clears_All_State()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        await fake.StartInstanceAsync("island_main");

        // Act
        fake.Reset();
        var result = await fake.ListInstancesAsync();

        // Assert
        result.Value.Should().BeEmpty();
        fake.WasMethodCalled(nameof(FakePokManagerClient.StartInstanceAsync)).Should().BeFalse();
    }

    [Fact]
    public async Task FakePokManagerClient_ListInstances_Returns_All_Setup_Instances()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        fake.SetupInstance("island_test", InstanceState.Stopped);
        fake.SetupInstance("island_dev", InstanceState.Running);

        // Act
        var result = await fake.ListInstancesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain("island_main");
        result.Value.Should().Contain("island_test");
        result.Value.Should().Contain("island_dev");
    }

    [Fact]
    public async Task FakePokManagerClient_GetInstanceDetails_Returns_Setup_Details()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        var details = new InstanceDetails(
            "island_main",
            "Main Island Server",
            InstanceState.Running,
            ProcessHealth.Healthy,
            8211,
            32,
            5,
            "1.2.3",
            TimeSpan.FromHours(2),
            "/opt/palworld/main",
            "/opt/palworld/main/world",
            "/opt/palworld/main/config",
            DateTimeOffset.UtcNow.AddDays(-7),
            DateTimeOffset.UtcNow.AddHours(-2),
            null,
            new Dictionary<string, string> { ["MaxPlayers"] = "32" });
        fake.SetupInstanceDetails("island_main", details);

        // Act
        var result = await fake.GetInstanceDetailsAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(details);
        result.Value.ServerName.Should().Be("Main Island Server");
        result.Value.Port.Should().Be(8211);
    }

    [Fact]
    public async Task FakePokManagerClient_StartInstance_Changes_State_To_Running()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Stopped);

        // Act
        var startResult = await fake.StartInstanceAsync("island_main");
        var statusResult = await fake.GetInstanceStatusAsync("island_main");

        // Assert
        startResult.IsSuccess.Should().BeTrue();
        statusResult.Value.State.Should().Be(InstanceState.Running);
    }

    [Fact]
    public async Task FakePokManagerClient_StopInstance_Changes_State_To_Stopped()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var stopResult = await fake.StopInstanceAsync("island_main");
        var statusResult = await fake.GetInstanceStatusAsync("island_main");

        // Assert
        stopResult.IsSuccess.Should().BeTrue();
        statusResult.Value.State.Should().Be(InstanceState.Stopped);
    }

    [Fact]
    public async Task FakePokManagerClient_CreateBackup_Adds_Backup_To_List()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var createResult = await fake.CreateBackupAsync("island_main", new CreateBackupOptions(
            Description: "Test backup",
            CompressionFormat: CompressionFormat.Gzip));
        var listResult = await fake.ListBackupsAsync("island_main");

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        listResult.IsSuccess.Should().BeTrue();
        listResult.Value.Should().HaveCount(1);
        listResult.Value[0].BackupId.Should().Be(createResult.Value);
        listResult.Value[0].Description.Should().Be("Test backup");
    }

    [Fact]
    public async Task FakePokManagerClient_DeleteBackup_Removes_Backup_From_List()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var backupId = (await fake.CreateBackupAsync("island_main")).Value;

        // Act
        var deleteResult = await fake.DeleteBackupAsync("island_main", backupId);
        var listResult = await fake.ListBackupsAsync("island_main");

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        listResult.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task FakePokManagerClient_ApplyConfiguration_Stores_Configuration()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var config = new Dictionary<string, string>
        {
            ["MaxPlayers"] = "32",
            ["ServerPassword"] = "secret"
        };

        // Act
        var applyResult = await fake.ApplyConfigurationAsync("island_main", config);
        var getResult = await fake.GetConfigurationAsync("island_main");

        // Assert
        applyResult.IsSuccess.Should().BeTrue();
        getResult.IsSuccess.Should().BeTrue();
        getResult.Value.Should().BeEquivalentTo(config);
    }

    [Fact]
    public async Task FakePokManagerClient_Returns_InstanceNotFound_For_NonExistent_Instance()
    {
        // Arrange
        var fake = new FakePokManagerClient();

        // Act
        var result = await fake.GetInstanceStatusAsync("nonexistent");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("InstanceNotFound");
    }

    [Fact]
    public async Task FakePokManagerClient_CreateInstance_Adds_New_Instance()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        var request = new CreateInstanceRequest(
            "new_island",
            "New Island",
            8211,
            32,
            "password",
            "adminpass",
            false);

        // Act
        var createResult = await fake.CreateInstanceAsync(request);
        var listResult = await fake.ListInstancesAsync();

        // Assert
        createResult.IsSuccess.Should().BeTrue();
        createResult.Value.Should().Be("new_island");
        listResult.Value.Should().Contain("new_island");
    }

    [Fact]
    public async Task FakePokManagerClient_DeleteInstance_Removes_Instance_And_Optionally_Backups()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Stopped);
        await fake.CreateBackupAsync("island_main");

        // Act
        var deleteResult = await fake.DeleteInstanceAsync("island_main", deleteBackups: true);
        var listResult = await fake.ListInstancesAsync();

        // Assert
        deleteResult.IsSuccess.Should().BeTrue();
        listResult.Value.Should().NotContain("island_main");
    }

    [Fact]
    public async Task FakePokManagerClient_CheckForUpdates_Returns_Setup_Update_Info()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var updateInfo = new UpdateAvailability(
            true,
            "1.0.0",
            "1.1.0",
            "New features added",
            1024 * 1024 * 500,
            true,
            DateTimeOffset.UtcNow);
        fake.SetupUpdateInfo("island_main", updateInfo);

        // Act
        var result = await fake.CheckForUpdatesAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsUpdateAvailable.Should().BeTrue();
        result.Value.CurrentVersion.Should().Be("1.0.0");
        result.Value.LatestVersion.Should().Be("1.1.0");
    }

    [Fact]
    public async Task FakePokManagerClient_SendChatMessage_Requires_Running_Instance()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Stopped);

        // Act
        var result = await fake.SendChatMessageAsync("island_main", "Hello!");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("InstanceNotRunning");
    }

    [Fact]
    public async Task FakePokManagerClient_SaveWorld_Succeeds_For_Running_Instance()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var result = await fake.SaveWorldAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FakePokManagerClient_ExecuteCustomCommand_Returns_Response()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var result = await fake.ExecuteCustomCommandAsync("island_main", "info");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("executed successfully");
    }

    [Fact]
    public async Task FakePokManagerClient_GetLogs_Returns_Setup_Logs()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var logs = new List<LogEntry>
        {
            new LogEntry(
                DateTimeOffset.UtcNow,
                LogLevel.Information,
                "Server started",
                "PalworldServer",
                "island_main",
                null,
                null),
            new LogEntry(
                DateTimeOffset.UtcNow.AddMinutes(1),
                LogLevel.Warning,
                "High memory usage",
                "PalworldServer",
                "island_main",
                null,
                null)
        };
        fake.SetupLogs("island_main", logs);

        // Act
        var result = await fake.GetLogsAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Message.Should().Be("Server started");
    }

    [Fact]
    public async Task FakePokManagerClient_HealthCheck_Returns_Healthy_Status()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var result = await fake.HealthCheckAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsHealthy.Should().BeTrue();
        result.Value.Health.Should().Be(ProcessHealth.Healthy);
    }

    [Fact]
    public async Task FakePokManagerClient_StreamLogs_Yields_Log_Entries()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var logs = new List<LogEntry>
        {
            new LogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Log 1", null, "island_main", null, null),
            new LogEntry(DateTimeOffset.UtcNow, LogLevel.Information, "Log 2", null, "island_main", null, null)
        };
        fake.SetupLogs("island_main", logs);

        // Act
        var streamedLogs = new List<LogEntry>();
        await foreach (var log in fake.StreamLogsAsync("island_main"))
        {
            streamedLogs.Add(log);
        }

        // Assert
        streamedLogs.Should().HaveCount(2);
        streamedLogs[0].Message.Should().Be("Log 1");
        streamedLogs[1].Message.Should().Be("Log 2");
    }

    [Fact]
    public async Task FakePokManagerClient_Failure_Only_Affects_Next_Operation()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        fake.FailNextOperation("First failure");

        // Act
        var firstResult = await fake.GetInstanceStatusAsync("island_main");
        var secondResult = await fake.GetInstanceStatusAsync("island_main");

        // Assert
        firstResult.IsFailure.Should().BeTrue();
        firstResult.Error.Should().Contain("First failure");
        secondResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FakePokManagerClient_ClearDelay_Removes_Simulated_Delay()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        fake.SimulateDelay(TimeSpan.FromMilliseconds(100));
        fake.ClearDelay();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await fake.GetInstanceStatusAsync("island_main");
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    [Fact]
    public async Task FakePokManagerClient_CreateInstance_With_AutoStart_Sets_Running_State()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        var request = new CreateInstanceRequest(
            "auto_start",
            "Auto Start Server",
            8211,
            32,
            null,
            "admin",
            AutoStart: true);

        // Act
        await fake.CreateInstanceAsync(request);
        var statusResult = await fake.GetInstanceStatusAsync("auto_start");

        // Assert
        statusResult.Value.State.Should().Be(InstanceState.Running);
    }

    [Fact]
    public async Task FakePokManagerClient_DownloadBackup_Returns_Stream()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var backupId = (await fake.CreateBackupAsync("island_main")).Value;

        // Act
        var result = await fake.DownloadBackupAsync("island_main", backupId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeAssignableTo<Stream>();
    }

    [Fact]
    public async Task FakePokManagerClient_RestoreBackup_Succeeds_For_Existing_Backup()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var backupId = (await fake.CreateBackupAsync("island_main")).Value;

        // Act
        var result = await fake.RestoreBackupAsync("island_main", backupId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task FakePokManagerClient_RestartInstance_Always_Sets_Running_State()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Stopped);

        // Act
        var restartResult = await fake.RestartInstanceAsync("island_main");
        var statusResult = await fake.GetInstanceStatusAsync("island_main");

        // Assert
        restartResult.IsSuccess.Should().BeTrue();
        statusResult.Value.State.Should().Be(InstanceState.Running);
    }

    [Fact]
    public async Task FakePokManagerClient_ValidateConfiguration_Returns_Valid_Result()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);
        var config = new Dictionary<string, string> { ["MaxPlayers"] = "32" };

        // Act
        var result = await fake.ValidateConfigurationAsync("island_main", config);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsValid.Should().BeTrue();
        result.Value.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task FakePokManagerClient_ApplyUpdates_Returns_Success_Result()
    {
        // Arrange
        var fake = new FakePokManagerClient();
        fake.SetupInstance("island_main", InstanceState.Running);

        // Act
        var result = await fake.ApplyUpdatesAsync("island_main");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.NewVersion.Should().NotBeNullOrEmpty();
    }
}
