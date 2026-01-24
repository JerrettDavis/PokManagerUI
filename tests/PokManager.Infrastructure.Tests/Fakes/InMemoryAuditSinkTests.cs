using PokManager.Application.Models;
using PokManager.Application.Ports;

namespace PokManager.Infrastructure.Tests.Fakes;

public class InMemoryAuditSinkTests
{
    private readonly InMemoryAuditSink _auditSink = new();

    [Fact]
    public async Task EmitAsync_AddsEventToStore()
    {
        // Arrange
        var auditEvent = CreateAuditEvent("instance-1", "Start");

        // Act
        await _auditSink.EmitAsync(auditEvent);

        // Assert
        var events = _auditSink.GetAllEvents();
        Assert.Single(events);
        Assert.Equal(auditEvent.EventId, events[0].EventId);
    }

    [Fact]
    public async Task QueryAsync_WithNoFilters_ReturnsAllEvents()
    {
        // Arrange
        var event1 = CreateAuditEvent("instance-1", "Start");
        var event2 = CreateAuditEvent("instance-2", "Stop");
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task QueryAsync_FiltersByInstanceId()
    {
        // Arrange
        var event1 = CreateAuditEvent("instance-1", "Start");
        var event2 = CreateAuditEvent("instance-2", "Start");
        var event3 = CreateAuditEvent("instance-1", "Stop");
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(InstanceId: "instance-1"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, e => Assert.Equal("instance-1", e.InstanceId));
    }

    [Fact]
    public async Task QueryAsync_FiltersByOperationType()
    {
        // Arrange
        var event1 = CreateAuditEvent("instance-1", "Start");
        var event2 = CreateAuditEvent("instance-2", "Stop");
        var event3 = CreateAuditEvent("instance-3", "Start");
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(OperationType: "Start"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, e => Assert.Equal("Start", e.OperationType));
    }

    [Fact]
    public async Task QueryAsync_FiltersByStartTime()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var event1 = CreateAuditEvent("instance-1", "Start", now.AddHours(-2));
        var event2 = CreateAuditEvent("instance-2", "Start", now.AddHours(-1));
        var event3 = CreateAuditEvent("instance-3", "Start", now);
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(StartTime: now.AddMinutes(-90)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, e => Assert.True(e.PerformedAt >= now.AddMinutes(-90)));
    }

    [Fact]
    public async Task QueryAsync_FiltersByEndTime()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var event1 = CreateAuditEvent("instance-1", "Start", now.AddHours(-2));
        var event2 = CreateAuditEvent("instance-2", "Start", now.AddHours(-1));
        var event3 = CreateAuditEvent("instance-3", "Start", now);
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(EndTime: now.AddMinutes(-90)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.True(result.Value[0].PerformedAt <= now.AddMinutes(-90));
    }

    [Fact]
    public async Task QueryAsync_FiltersByTimeRange()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var event1 = CreateAuditEvent("instance-1", "Start", now.AddHours(-3));
        var event2 = CreateAuditEvent("instance-2", "Start", now.AddHours(-2));
        var event3 = CreateAuditEvent("instance-3", "Start", now.AddHours(-1));
        var event4 = CreateAuditEvent("instance-4", "Start", now);
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);
        await _auditSink.EmitAsync(event4);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(
            StartTime: now.AddHours(-2.5),
            EndTime: now.AddMinutes(-30)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task QueryAsync_RespectsMaxResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            var evt = CreateAuditEvent($"instance-{i}", "Start");
            await _auditSink.EmitAsync(evt);
        }

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(MaxResults: 5));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
    }

    [Fact]
    public async Task QueryAsync_ReturnsEventsOrderedByPerformedAtDescending()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var event1 = CreateAuditEvent("instance-1", "Start", now.AddHours(-2));
        var event2 = CreateAuditEvent("instance-2", "Start", now.AddHours(-1));
        var event3 = CreateAuditEvent("instance-3", "Start", now);
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);
        Assert.True(result.Value[0].PerformedAt >= result.Value[1].PerformedAt);
        Assert.True(result.Value[1].PerformedAt >= result.Value[2].PerformedAt);
    }

    [Fact]
    public async Task QueryAsync_CombinesMultipleFilters()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var event1 = CreateAuditEvent("instance-1", "Start", now.AddHours(-2));
        var event2 = CreateAuditEvent("instance-1", "Stop", now.AddHours(-1));
        var event3 = CreateAuditEvent("instance-2", "Start", now.AddMinutes(-30));
        var event4 = CreateAuditEvent("instance-1", "Start", now);
        await _auditSink.EmitAsync(event1);
        await _auditSink.EmitAsync(event2);
        await _auditSink.EmitAsync(event3);
        await _auditSink.EmitAsync(event4);

        // Act
        var result = await _auditSink.QueryAsync(new AuditQuery(
            InstanceId: "instance-1",
            OperationType: "Start",
            StartTime: now.AddHours(-3)));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.All(result.Value, e =>
        {
            Assert.Equal("instance-1", e.InstanceId);
            Assert.Equal("Start", e.OperationType);
        });
    }

    [Fact]
    public void Reset_ClearsAllEvents()
    {
        // Arrange
        var event1 = CreateAuditEvent("instance-1", "Start");
        var event2 = CreateAuditEvent("instance-2", "Stop");
        _auditSink.EmitAsync(event1).Wait();
        _auditSink.EmitAsync(event2).Wait();

        // Act
        _auditSink.Reset();

        // Assert
        var events = _auditSink.GetAllEvents();
        Assert.Empty(events);
    }

    [Fact]
    public async Task AuditSink_IsThreadSafe_WhenEmittingEventsConcurrently()
    {
        // Arrange
        const int taskCount = 100;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < taskCount; i++)
        {
            var instanceId = $"instance-{i}";
            tasks.Add(Task.Run(async () =>
            {
                var evt = CreateAuditEvent(instanceId, "Start");
                await _auditSink.EmitAsync(evt);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var events = _auditSink.GetAllEvents();
        Assert.Equal(taskCount, events.Count);
    }

    private static AuditEvent CreateAuditEvent(
        string instanceId,
        string operationType,
        DateTimeOffset? performedAt = null)
    {
        return new AuditEvent(
            EventId: Guid.NewGuid(),
            InstanceId: instanceId,
            OperationType: operationType,
            PerformedBy: "test-user",
            PerformedAt: performedAt ?? DateTimeOffset.UtcNow,
            Outcome: "Success",
            Duration: TimeSpan.FromSeconds(1),
            Details: null,
            ErrorMessage: null
        );
    }
}
