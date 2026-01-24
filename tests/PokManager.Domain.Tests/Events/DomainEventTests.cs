using FluentAssertions;
using PokManager.Domain.Events;

namespace PokManager.Domain.Tests.Events;

public class DomainEventTests
{
    [Fact]
    public void InstanceCreatedEvent_Should_Have_EventId()
    {
        // Arrange & Act
        var evt = new InstanceCreatedEvent("island_main", "My Server", "TheIsland");

        // Assert
        evt.EventId.Should().NotBe(Guid.Empty);
        evt.OccurredAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void InstanceCreatedEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string instanceId = "island_main";
        const string sessionName = "My Server";
        const string mapName = "TheIsland";

        // Act
        var evt = new InstanceCreatedEvent(instanceId, sessionName, mapName);

        // Assert
        evt.InstanceId.Should().Be(instanceId);
        evt.SessionName.Should().Be(sessionName);
        evt.MapName.Should().Be(mapName);
    }

    [Fact]
    public void InstanceStartedEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string instanceId = "island_main";
        var startedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new InstanceStartedEvent(instanceId, startedAt);

        // Assert
        evt.InstanceId.Should().Be(instanceId);
        evt.StartedAt.Should().Be(startedAt);
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void InstanceStoppedEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string instanceId = "island_main";
        var stoppedAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new InstanceStoppedEvent(instanceId, stoppedAt);

        // Assert
        evt.InstanceId.Should().Be(instanceId);
        evt.StoppedAt.Should().Be(stoppedAt);
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void BackupCreatedEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string backupId = "backup_20260119_120000";
        const string instanceId = "island_main";
        const long backupSize = 1024000;

        // Act
        var evt = new BackupCreatedEvent(backupId, instanceId, backupSize);

        // Assert
        evt.BackupId.Should().Be(backupId);
        evt.InstanceId.Should().Be(instanceId);
        evt.BackupSize.Should().Be(backupSize);
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void BackupRestoredEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string backupId = "backup_20260119_120000";
        const string instanceId = "island_main";
        var restoredAt = DateTimeOffset.UtcNow;

        // Act
        var evt = new BackupRestoredEvent(backupId, instanceId, restoredAt);

        // Assert
        evt.BackupId.Should().Be(backupId);
        evt.InstanceId.Should().Be(instanceId);
        evt.RestoredAt.Should().Be(restoredAt);
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ConfigurationAppliedEvent_Should_Store_Properties_Correctly()
    {
        // Arrange
        const string instanceId = "island_main";
        const string configKey = "MaxPlayers";
        const string configValue = "10";

        // Act
        var evt = new ConfigurationAppliedEvent(instanceId, configKey, configValue);

        // Assert
        evt.InstanceId.Should().Be(instanceId);
        evt.ConfigurationKey.Should().Be(configKey);
        evt.ConfigurationValue.Should().Be(configValue);
        evt.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void DomainEvents_Should_Have_Unique_EventIds()
    {
        // Arrange & Act
        var evt1 = new InstanceCreatedEvent("id1", "name1", "map1");
        var evt2 = new InstanceCreatedEvent("id2", "name2", "map2");

        // Assert
        evt1.EventId.Should().NotBe(evt2.EventId);
    }

    [Fact]
    public void DomainEvents_Should_Have_OccurredAt_Set()
    {
        // Arrange & Act
        var before = DateTimeOffset.UtcNow;
        var evt = new InstanceStartedEvent("test_id", DateTimeOffset.UtcNow);
        var after = DateTimeOffset.UtcNow;

        // Assert
        evt.OccurredAt.Should().BeOnOrAfter(before);
        evt.OccurredAt.Should().BeOnOrBefore(after);
    }
}
