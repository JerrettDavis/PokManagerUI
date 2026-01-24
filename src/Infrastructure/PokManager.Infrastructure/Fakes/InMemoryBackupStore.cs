using PokManager.Application.Ports;
using PokManager.Application.Models;
using PokManager.Domain.Common;
using System.Collections.Concurrent;

namespace PokManager.Infrastructure.Fakes;

public class InMemoryBackupStore : IBackupStore
{
    private readonly ConcurrentDictionary<string, List<BackupInfo>> _backups = new();
    private readonly ConcurrentDictionary<string, byte[]> _backupData = new();

    public Task<Result<IReadOnlyList<BackupInfo>>> ListBackupsAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        // If instanceId is null or empty, return all backups across all instances
        if (string.IsNullOrEmpty(instanceId))
        {
            var allBackups = new List<BackupInfo>();
            foreach (var kvp in _backups)
            {
                lock (kvp.Value)
                {
                    allBackups.AddRange(kvp.Value);
                }
            }

            var sortedBackups = allBackups
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Success(sortedBackups.AsReadOnly()));
        }

        if (!_backups.TryGetValue(instanceId, out var backupList))
            return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Success(Array.Empty<BackupInfo>()));

        List<BackupInfo> backups;
        lock (backupList)
        {
            backups = backupList
                .OrderByDescending(b => b.CreatedAt)
                .ToList();
        }

        return Task.FromResult(Result<IReadOnlyList<BackupInfo>>.Success(backups.AsReadOnly()));
    }

    public Task<Result<Stream>> GetBackupStreamAsync(
        string instanceId,
        string backupId,
        CancellationToken cancellationToken = default)
    {
        if (!_backups.TryGetValue(instanceId, out var backupList))
            return Task.FromResult(Result.Failure<Stream>("InstanceNotFound"));

        BackupInfo? backup;
        lock (backupList)
        {
            backup = backupList.FirstOrDefault(b => b.BackupId == backupId);
        }

        if (backup == null)
            return Task.FromResult(Result.Failure<Stream>("BackupNotFound"));

        // Return stored backup data or empty stream
        var key = $"{instanceId}:{backupId}";
        var data = _backupData.TryGetValue(key, out var bytes) ? bytes : Array.Empty<byte>();
        var stream = new MemoryStream(data);

        return Task.FromResult(Result<Stream>.Success(stream as Stream));
    }

    public Task<Result<Unit>> DeleteOldestBackupsAsync(
        string instanceId,
        int keepCount,
        CancellationToken cancellationToken = default)
    {
        if (!_backups.TryGetValue(instanceId, out var backupList))
            return Task.FromResult(Result.Success());

        List<BackupInfo> toDelete;
        lock (backupList)
        {
            var backups = backupList
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            if (backups.Count <= keepCount)
                return Task.FromResult(Result.Success());

            toDelete = backups.Skip(keepCount).ToList();
            foreach (var backup in toDelete)
            {
                backupList.Remove(backup);
            }
        }

        // Remove data outside the lock
        foreach (var backup in toDelete)
        {
            var key = $"{instanceId}:{backup.BackupId}";
            _backupData.TryRemove(key, out _);
        }

        return Task.FromResult(Result.Success());
    }

    public Task<Result<long>> GetTotalBackupSizeAsync(
        string instanceId,
        CancellationToken cancellationToken = default)
    {
        if (!_backups.TryGetValue(instanceId, out var backupList))
            return Task.FromResult(Result<long>.Success(0));

        long totalSize;
        lock (backupList)
        {
            totalSize = backupList.Sum(b => b.SizeInBytes);
        }

        return Task.FromResult(Result<long>.Success(totalSize));
    }

    public Task<Result<Unit>> StoreBackupAsync(
        BackupInfo backupInfo,
        CancellationToken cancellationToken = default)
    {
        var backupList = _backups.GetOrAdd(backupInfo.InstanceId, _ => new List<BackupInfo>());

        lock (backupList)
        {
            backupList.Add(backupInfo);
        }

        return Task.FromResult(Result.Success());
    }

    // Helper methods for tests
    public void AddBackup(string instanceId, BackupInfo backup, byte[]? data = null)
    {
        var backupList = _backups.GetOrAdd(instanceId, _ => new List<BackupInfo>());

        lock (backupList)
        {
            backupList.Add(backup);
        }

        if (data != null)
        {
            var key = $"{instanceId}:{backup.BackupId}";
            _backupData[key] = data;
        }
    }

    public void Reset()
    {
        _backups.Clear();
        _backupData.Clear();
    }
}
