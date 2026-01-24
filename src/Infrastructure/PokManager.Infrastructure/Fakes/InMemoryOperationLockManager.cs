using PokManager.Application.Ports;
using PokManager.Domain.Common;
using System.Collections.Concurrent;

namespace PokManager.Infrastructure.Fakes;

public class InMemoryOperationLockManager : IOperationLockManager
{
    private readonly ConcurrentDictionary<string, OperationLock> _locks = new();

    public Task<Result<IOperationLock>> AcquireLockAsync(
        string instanceId,
        string operationId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (_locks.TryGetValue(instanceId, out var existingLock))
        {
            return Task.FromResult(Result.Failure<IOperationLock>(
                $"Instance {instanceId} is locked by operation {existingLock.OperationId}"));
        }

        var newLock = new OperationLock(instanceId, operationId, this);
        _locks[instanceId] = newLock;

        return Task.FromResult(Result<IOperationLock>.Success(newLock));
    }

    public Task<bool> IsLockedAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_locks.ContainsKey(instanceId));
    }

    public Task<string?> GetLockingOperationIdAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        if (_locks.TryGetValue(instanceId, out var lockInfo))
            return Task.FromResult<string?>(lockInfo.OperationId);

        return Task.FromResult<string?>(null);
    }

    internal void ReleaseLock(string instanceId)
    {
        _locks.TryRemove(instanceId, out _);
    }

    public void Reset()
    {
        _locks.Clear();
    }

    private class OperationLock(string instanceId, string operationId, InMemoryOperationLockManager manager)
        : IOperationLock
    {
        private bool _disposed;

        public string InstanceId { get; } = instanceId;
        public string OperationId { get; } = operationId;
        public DateTimeOffset AcquiredAt { get; } = DateTimeOffset.UtcNow;

        public ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                manager.ReleaseLock(InstanceId);
                _disposed = true;
            }
            return ValueTask.CompletedTask;
        }
    }
}
