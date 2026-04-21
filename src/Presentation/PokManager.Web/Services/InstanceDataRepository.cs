using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PokManager.Web.Data;
using PokManager.Web.Data.Entities;
using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// Repository for persisting and retrieving instance data from the database.
/// </summary>
public class InstanceDataRepository
{
    private readonly IDbContextFactory<PokManagerDbContext> _contextFactory;
    private readonly ILogger<InstanceDataRepository> _logger;

    public InstanceDataRepository(
        IDbContextFactory<PokManagerDbContext> contextFactory,
        ILogger<InstanceDataRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task SaveInstanceSnapshotAsync(InstanceViewModel instance, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var snapshot = new InstanceSnapshot
        {
            InstanceId = instance.Id,
            Name = instance.Name,
            Timestamp = DateTime.UtcNow,
            Status = instance.Status.ToString(),
            Health = instance.Health.ToString(),
            Uptime = instance.Uptime,
            StartedAt = instance.StartedAt,
            ServerMap = instance.ServerMap,
            MaxPlayers = instance.MaxPlayers,
            CurrentPlayers = instance.CurrentPlayers,
            Port = instance.Port,
            Version = instance.Version,
            IsPublic = instance.IsPublic,
            IsPvE = instance.IsPvE,
            CpuUsagePercent = instance.CpuUsagePercent,
            MemoryUsageMB = instance.MemoryUsageMB
        };

        context.InstanceSnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Saved snapshot for instance {InstanceId}", instance.Id);
    }

    public async Task SavePlayerSessionsAsync(string instanceId, List<InstanceDataCache.PlayerInfo> players, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Mark all existing sessions as offline first
        var existingSessions = await context.PlayerSessions
            .Where(p => p.InstanceId == instanceId && p.IsOnline)
            .ToListAsync(cancellationToken);

        foreach (var session in existingSessions)
        {
            var stillOnline = players.Any(p => p.SteamId == session.SteamId);
            if (!stillOnline)
            {
                session.IsOnline = false;
                session.LeftAt = DateTime.UtcNow;
            }
        }

        // Add or update current players
        foreach (var player in players)
        {
            var existingSession = await context.PlayerSessions
                .FirstOrDefaultAsync(p => p.InstanceId == instanceId
                    && p.SteamId == player.SteamId
                    && p.IsOnline, cancellationToken);

            if (existingSession == null)
            {
                // New session
                context.PlayerSessions.Add(new PlayerSession
                {
                    InstanceId = instanceId,
                    PlayerName = player.Name,
                    SteamId = player.SteamId,
                    JoinedAt = player.JoinedAt,
                    Level = player.Level,
                    Location = player.Location,
                    IsOnline = true
                });
            }
            else
            {
                // Update existing session
                existingSession.Level = player.Level;
                existingSession.Location = player.Location;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("Saved {Count} player sessions for instance {InstanceId}", players.Count, instanceId);
    }

    public async Task SaveTelemetrySnapshotAsync(string instanceId, InstanceDataCache.InstanceTelemetry telemetry, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var snapshot = new TelemetrySnapshot
        {
            InstanceId = instanceId,
            Timestamp = telemetry.Timestamp,
            CpuUsagePercent = telemetry.CpuUsagePercent,
            MemoryUsageMB = telemetry.MemoryUsageMB,
            NetworkInKBps = telemetry.NetworkInKBps,
            NetworkOutKBps = telemetry.NetworkOutKBps,
            Fps = telemetry.Fps,
            TickRate = telemetry.TickRate,
            GameStatsJson = JsonSerializer.Serialize(telemetry.GameStats)
        };

        context.TelemetrySnapshots.Add(snapshot);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Saved telemetry for instance {InstanceId}", instanceId);
    }

    public async Task SaveLogEntriesAsync(string instanceId, List<InstanceDataCache.LogEntry> logs, CancellationToken cancellationToken = default)
    {
        if (!logs.Any()) return;

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Check for duplicates by timestamp and message
        var existingHashes = await context.LogEntries
            .Where(l => l.InstanceId == instanceId)
            .OrderByDescending(l => l.Timestamp)
            .Take(100)
            .Select(l => new { l.Timestamp, l.Message })
            .ToListAsync(cancellationToken);

        var newLogs = logs
            .Where(log => !existingHashes.Any(e =>
                e.Timestamp == log.Timestamp && e.Message == log.Message))
            .Select(log => new Data.Entities.LogEntry
            {
                InstanceId = instanceId,
                Timestamp = log.Timestamp,
                Level = log.Level,
                Message = log.Message,
                Source = "docker"
            })
            .ToList();

        if (newLogs.Any())
        {
            context.LogEntries.AddRange(newLogs);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("Saved {Count} new log entries for instance {InstanceId}", newLogs.Count, instanceId);
        }
    }

    public async Task<List<Data.Entities.LogEntry>> GetRecentLogsAsync(string instanceId, int count = 100, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.LogEntries
            .Where(l => l.InstanceId == instanceId)
            .OrderByDescending(l => l.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PlayerSession>> GetOnlinePlayersAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.PlayerSessions
            .Where(p => p.InstanceId == instanceId && p.IsOnline)
            .OrderBy(p => p.JoinedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<TelemetrySnapshot?> GetLatestTelemetryAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.TelemetrySnapshots
            .Where(t => t.InstanceId == instanceId)
            .OrderByDescending(t => t.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CleanupOldDataAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var cutoffDate = DateTime.UtcNow - retentionPeriod;

        // Clean up old snapshots
        var oldSnapshots = await context.InstanceSnapshots
            .Where(s => s.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);
        context.InstanceSnapshots.RemoveRange(oldSnapshots);

        // Clean up old telemetry
        var oldTelemetry = await context.TelemetrySnapshots
            .Where(t => t.Timestamp < cutoffDate)
            .ToListAsync(cancellationToken);
        context.TelemetrySnapshots.RemoveRange(oldTelemetry);

        // Clean up old logs (keep only 7 days)
        var logCutoff = DateTime.UtcNow.AddDays(-7);
        var oldLogs = await context.LogEntries
            .Where(l => l.Timestamp < logCutoff)
            .ToListAsync(cancellationToken);
        context.LogEntries.RemoveRange(oldLogs);

        // Clean up old offline player sessions (keep only 30 days)
        var sessionCutoff = DateTime.UtcNow.AddDays(-30);
        var oldSessions = await context.PlayerSessions
            .Where(p => !p.IsOnline && p.LeftAt < sessionCutoff)
            .ToListAsync(cancellationToken);
        context.PlayerSessions.RemoveRange(oldSessions);

        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cleaned up old data: {Snapshots} snapshots, {Telemetry} telemetry, {Logs} logs, {Sessions} sessions",
            oldSnapshots.Count, oldTelemetry.Count, oldLogs.Count, oldSessions.Count);
    }
}
