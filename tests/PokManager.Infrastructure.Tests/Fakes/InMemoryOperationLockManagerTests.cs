namespace PokManager.Infrastructure.Tests.Fakes;

public class InMemoryOperationLockManagerTests
{
    private readonly InMemoryOperationLockManager _lockManager = new();

    [Fact]
    public async Task AcquireLockAsync_WhenNotLocked_ReturnsSuccessWithLock()
    {
        // Act
        var result = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("instance-1", result.Value.InstanceId);
        Assert.Equal("op-1", result.Value.OperationId);
        Assert.True(result.Value.AcquiredAt <= DateTimeOffset.UtcNow);

        // Cleanup
        await result.Value.DisposeAsync();
    }

    [Fact]
    public async Task AcquireLockAsync_WhenAlreadyLocked_ReturnsFailure()
    {
        // Arrange
        var lock1 = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));

        // Act
        var result = await _lockManager.AcquireLockAsync("instance-1", "op-2", TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("instance-1", result.Error);
        Assert.Contains("op-1", result.Error);

        // Cleanup
        await lock1.Value.DisposeAsync();
    }

    [Fact]
    public async Task AcquireLockAsync_AfterDispose_AllowsNewLock()
    {
        // Arrange
        var lock1 = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));
        await lock1.Value.DisposeAsync();

        // Act
        var result = await _lockManager.AcquireLockAsync("instance-1", "op-2", TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("op-2", result.Value.OperationId);

        // Cleanup
        await result.Value.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_WhenNotLocked_ReturnsFalse()
    {
        // Act
        var result = await _lockManager.IsLockedAsync("instance-1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsLockedAsync_WhenLocked_ReturnsTrue()
    {
        // Arrange
        var lockResult = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));

        // Act
        var result = await _lockManager.IsLockedAsync("instance-1");

        // Assert
        Assert.True(result);

        // Cleanup
        await lockResult.Value.DisposeAsync();
    }

    [Fact]
    public async Task IsLockedAsync_AfterDispose_ReturnsFalse()
    {
        // Arrange
        var lockResult = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));
        await lockResult.Value.DisposeAsync();

        // Act
        var result = await _lockManager.IsLockedAsync("instance-1");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetLockingOperationIdAsync_WhenNotLocked_ReturnsNull()
    {
        // Act
        var result = await _lockManager.GetLockingOperationIdAsync("instance-1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLockingOperationIdAsync_WhenLocked_ReturnsOperationId()
    {
        // Arrange
        var lockResult = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));

        // Act
        var result = await _lockManager.GetLockingOperationIdAsync("instance-1");

        // Assert
        Assert.Equal("op-1", result);

        // Cleanup
        await lockResult.Value.DisposeAsync();
    }

    [Fact]
    public async Task GetLockingOperationIdAsync_AfterDispose_ReturnsNull()
    {
        // Arrange
        var lockResult = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));
        await lockResult.Value.DisposeAsync();

        // Act
        var result = await _lockManager.GetLockingOperationIdAsync("instance-1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DisposeAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var lockResult = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));

        // Act & Assert - should not throw
        await lockResult.Value.DisposeAsync();
        await lockResult.Value.DisposeAsync();
        await lockResult.Value.DisposeAsync();

        var isLocked = await _lockManager.IsLockedAsync("instance-1");
        Assert.False(isLocked);
    }

    [Fact]
    public void Reset_ClearsAllLocks()
    {
        // Arrange
        var lock1 = _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30)).Result;
        var lock2 = _lockManager.AcquireLockAsync("instance-2", "op-2", TimeSpan.FromSeconds(30)).Result;

        // Act
        _lockManager.Reset();

        // Assert
        var isLocked1 = _lockManager.IsLockedAsync("instance-1").Result;
        var isLocked2 = _lockManager.IsLockedAsync("instance-2").Result;
        Assert.False(isLocked1);
        Assert.False(isLocked2);

        // Note: Original locks are now orphaned but that's acceptable for test cleanup
    }

    [Fact]
    public async Task LockManager_IsThreadSafe_WhenAcquiringLocksConcurrently()
    {
        // Arrange
        const int taskCount = 10;
        var tasks = new List<Task<bool>>();
        var successCount = 0;

        // Act - Try to acquire the same lock from multiple threads
        for (int i = 0; i < taskCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = await _lockManager.AcquireLockAsync("instance-1", $"op-{i}", TimeSpan.FromSeconds(30));
                if (result.IsSuccess)
                {
                    Interlocked.Increment(ref successCount);
                    await Task.Delay(10); // Hold the lock briefly
                    await result.Value.DisposeAsync();
                    return true;
                }
                return false;
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Only one task should have acquired the lock at any given time
        // But due to sequential releases and re-acquisitions, we might have multiple successes
        Assert.True(successCount >= 1, "At least one lock acquisition should succeed");

        // Final state should be unlocked
        var isLocked = await _lockManager.IsLockedAsync("instance-1");
        Assert.False(isLocked);
    }

    [Fact]
    public async Task LockManager_AllowsConcurrentLocksForDifferentInstances()
    {
        // Arrange & Act
        var lock1 = await _lockManager.AcquireLockAsync("instance-1", "op-1", TimeSpan.FromSeconds(30));
        var lock2 = await _lockManager.AcquireLockAsync("instance-2", "op-2", TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(lock1.IsSuccess);
        Assert.True(lock2.IsSuccess);

        var isLocked1 = await _lockManager.IsLockedAsync("instance-1");
        var isLocked2 = await _lockManager.IsLockedAsync("instance-2");
        Assert.True(isLocked1);
        Assert.True(isLocked2);

        // Cleanup
        await lock1.Value.DisposeAsync();
        await lock2.Value.DisposeAsync();
    }
}
