using System.Collections.Concurrent;
using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// In-memory cache for instance data, player information, and telemetry.
/// </summary>
public class InstanceDataCache
{
    private readonly ConcurrentDictionary<string, CachedInstanceData> _instanceData = new();
    private readonly ConcurrentDictionary<string, List<LogEntry>> _instanceLogs = new();
    private readonly ConcurrentDictionary<string, List<PlayerInfo>> _instancePlayers = new();
    private readonly ConcurrentDictionary<string, InstanceTelemetry> _instanceTelemetry = new();

    public void UpdateInstanceData(string instanceId, InstanceViewModel instance)
    {
        var cached = new CachedInstanceData
        {
            Instance = instance,
            LastUpdated = DateTime.UtcNow
        };

        _instanceData.AddOrUpdate(instanceId, cached, (_, __) => cached);
    }

    public InstanceViewModel? GetInstance(string instanceId)
    {
        return _instanceData.TryGetValue(instanceId, out var cached) ? cached.Instance : null;
    }

    public List<InstanceViewModel> GetAllInstances()
    {
        return _instanceData.Values
            .OrderBy(x => x.Instance.Name)
            .Select(x => x.Instance)
            .ToList();
    }

    public void AddLogs(string instanceId, IEnumerable<LogEntry> logs)
    {
        _instanceLogs.AddOrUpdate(
            instanceId,
            logs.ToList(),
            (_, existing) =>
            {
                var combined = existing.Concat(logs).ToList();
                // Keep only the last 500 log entries
                return combined.TakeLast(500).ToList();
            });
    }

    public List<LogEntry> GetLogs(string instanceId, int count = 100)
    {
        if (_instanceLogs.TryGetValue(instanceId, out var logs))
        {
            return logs.TakeLast(count).ToList();
        }
        return new List<LogEntry>();
    }

    public void UpdatePlayers(string instanceId, List<PlayerInfo> players)
    {
        _instancePlayers.AddOrUpdate(instanceId, players, (_, __) => players);
    }

    public List<PlayerInfo> GetPlayers(string instanceId)
    {
        return _instancePlayers.TryGetValue(instanceId, out var players) ? players : new List<PlayerInfo>();
    }

    public void UpdateTelemetry(string instanceId, InstanceTelemetry telemetry)
    {
        _instanceTelemetry.AddOrUpdate(instanceId, telemetry, (_, __) => telemetry);
    }

    public InstanceTelemetry? GetTelemetry(string instanceId)
    {
        return _instanceTelemetry.TryGetValue(instanceId, out var telemetry) ? telemetry : null;
    }

    public class CachedInstanceData
    {
        public InstanceViewModel Instance { get; set; } = null!;
        public DateTime LastUpdated { get; set; }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class PlayerInfo
    {
        public string Name { get; set; } = string.Empty;
        public string SteamId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public string? Location { get; set; }
        public int Level { get; set; }
    }

    public class InstanceTelemetry
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercent { get; set; }
        public long MemoryUsageMB { get; set; }
        public long NetworkInKBps { get; set; }
        public long NetworkOutKBps { get; set; }
        public int Fps { get; set; }
        public int TickRate { get; set; }
        public Dictionary<string, object> GameStats { get; set; } = new();
    }
}
