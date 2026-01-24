using PokManager.Web.Models;

namespace PokManager.Web.Services;

/// <summary>
/// Background worker that periodically fetches instance data and updates the cache.
/// </summary>
public class InstanceDataWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly InstanceDataCache _cache;
    private readonly ILogger<InstanceDataWorker> _logger;
    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(60); // Update every 60 seconds
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // Cleanup daily
    private DateTime _lastCleanup = DateTime.UtcNow;

    public InstanceDataWorker(
        IServiceProvider serviceProvider,
        InstanceDataCache cache,
        ILogger<InstanceDataWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Instance Data Worker started");

        // Wait a bit before first run to let the application start up
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateInstanceDataAsync(stoppingToken);
                
                // Run cleanup if needed
                if (DateTime.UtcNow - _lastCleanup > _cleanupInterval)
                {
                    await CleanupOldDataAsync(stoppingToken);
                    _lastCleanup = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating instance data");
            }

            try
            {
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when shutting down
                break;
            }
        }

        _logger.LogInformation("Instance Data Worker stopped");
    }

    private async Task UpdateInstanceDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var instanceService = scope.ServiceProvider.GetRequiredService<InstanceService>();

        _logger.LogDebug("Fetching instance data from API");

        var result = await instanceService.GetAllInstancesAsync(cancellationToken);

        if (result.IsSuccess && result.Value != null)
        {
            _logger.LogInformation("Successfully fetched {Count} instances", result.Value.Count);

            foreach (var instance in result.Value)
            {
                // Update the cache with the latest data
                _cache.UpdateInstanceData(instance.Id, instance);

                // Fetch additional data for running instances
                if (instance.Status == InstanceStatus.Running)
                {
                    await UpdateInstanceDetailsAsync(instance.Id, cancellationToken);
                }
            }
        }
        else
        {
            _logger.LogWarning("Failed to fetch instances: {Error}", result.Error);
        }
    }

    private async Task UpdateInstanceDetailsAsync(string instanceId, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<InstanceDataRepository>();
        var instanceService = scope.ServiceProvider.GetRequiredService<InstanceService>();
        var rconService = scope.ServiceProvider.GetRequiredService<RconService>();

        // Fetch real Docker logs from API
        var logs = await FetchRealLogsAsync(instanceId, instanceService, cancellationToken);
        if (logs.Any())
        {
            _cache.AddLogs(instanceId, logs);
            await repository.SaveLogEntriesAsync(instanceId, logs, cancellationToken);
        }

        // Fetch real player data from RCON
        var players = await FetchRealPlayersAsync(instanceId, rconService, cancellationToken);
        if (players.Any())
        {
            _cache.UpdatePlayers(instanceId, players);
            await repository.SavePlayerSessionsAsync(instanceId, players, cancellationToken);
        }
        else
        {
            // If RCON fails or no players, use empty list (don't use mock data)
            _cache.UpdatePlayers(instanceId, new List<InstanceDataCache.PlayerInfo>());
        }

        // Mock telemetry data (TODO: fetch from docker stats or game server)
        var telemetry = GenerateMockTelemetry(instanceId);
        _cache.UpdateTelemetry(instanceId, telemetry);
        await repository.SaveTelemetrySnapshotAsync(instanceId, telemetry, cancellationToken);
    }

    private async Task<List<InstanceDataCache.PlayerInfo>> FetchRealPlayersAsync(
        string instanceId,
        RconService rconService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Fetching real player data via RCON for instance {InstanceId}", instanceId);
            
            var players = await rconService.GetOnlinePlayersAsync(instanceId, cancellationToken);
            
            if (players.Any())
            {
                _logger.LogInformation("Found {Count} players online for instance {InstanceId}", players.Count, instanceId);
            }
            
            return players;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching real player data for instance {InstanceId}", instanceId);
            return new List<InstanceDataCache.PlayerInfo>();
        }
    }

    private async Task<List<InstanceDataCache.LogEntry>> FetchRealLogsAsync(
        string instanceId, 
        InstanceService instanceService,
        CancellationToken cancellationToken)
    {
        try
        {
            // Use the configured InstanceService HttpClient which has the correct base address
            using var scope = _serviceProvider.CreateScope();
            var httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            using var httpClient = httpClientFactory.CreateClient(nameof(InstanceService));
            
            var response = await httpClient.GetAsync($"/api/instances/{instanceId}/logs?tail=50", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch logs for instance {InstanceId}: {StatusCode}", instanceId, response.StatusCode);
                return new List<InstanceDataCache.LogEntry>();
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<LogsResponse>(cancellationToken);
            if (jsonResponse?.Logs == null)
            {
                return new List<InstanceDataCache.LogEntry>();
            }

            // Parse Docker logs into structured log entries
            return ParseDockerLogs(jsonResponse.Logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching real logs for instance {InstanceId}", instanceId);
            return new List<InstanceDataCache.LogEntry>();
        }
    }

    private List<InstanceDataCache.LogEntry> ParseDockerLogs(string dockerLogs)
    {
        var entries = new List<InstanceDataCache.LogEntry>();
        var lines = dockerLogs.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Try to parse timestamp and level from Docker logs
            // Format varies but often includes timestamps
            var entry = new InstanceDataCache.LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = DetermineLogLevel(line),
                Message = line.Trim()
            };

            // Try to extract timestamp if present (common formats)
            if (TryExtractTimestamp(line, out var timestamp))
            {
                entry.Timestamp = timestamp;
            }

            entries.Add(entry);
        }

        return entries;
    }

    private bool TryExtractTimestamp(string line, out DateTime timestamp)
    {
        timestamp = DateTime.UtcNow;

        // Try common timestamp formats
        // ISO 8601: 2024-01-20T12:34:56Z
        var isoMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z?)");
        if (isoMatch.Success && DateTime.TryParse(isoMatch.Groups[1].Value, out timestamp))
        {
            return true;
        }

        // Standard date format: 2024-01-20 12:34:56
        var standardMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})");
        if (standardMatch.Success && DateTime.TryParse(standardMatch.Groups[1].Value, out timestamp))
        {
            return true;
        }

        return false;
    }

    private string DetermineLogLevel(string line)
    {
        var lowerLine = line.ToLowerInvariant();
        
        if (lowerLine.Contains("error") || lowerLine.Contains("fatal") || lowerLine.Contains("exception"))
            return "ERROR";
        if (lowerLine.Contains("warn") || lowerLine.Contains("warning"))
            return "WARN";
        if (lowerLine.Contains("debug") || lowerLine.Contains("trace"))
            return "DEBUG";
        
        return "INFO";
    }

    private record LogsResponse(string InstanceId, string ContainerId, string Logs);

    private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<InstanceDataRepository>();
            
            // Keep 30 days of historical data
            await repository.CleanupOldDataAsync(TimeSpan.FromDays(30), cancellationToken);
            
            _logger.LogInformation("Completed database cleanup");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database cleanup");
        }
    }



    private InstanceDataCache.InstanceTelemetry GenerateMockTelemetry(string instanceId)
    {
        var random = new Random(instanceId.GetHashCode() + DateTime.UtcNow.Minute);

        return new InstanceDataCache.InstanceTelemetry
        {
            Timestamp = DateTime.UtcNow,
            CpuUsagePercent = random.Next(20, 80) + random.NextDouble(),
            MemoryUsageMB = random.Next(2048, 6144),
            NetworkInKBps = random.Next(100, 5000),
            NetworkOutKBps = random.Next(50, 2000),
            Fps = random.Next(50, 60),
            TickRate = random.Next(25, 30),
            GameStats = new Dictionary<string, object>
            {
                { "WildPals", random.Next(1000, 5000) },
                { "BasePals", random.Next(100, 500) },
                { "CapturedPals", random.Next(50, 200) },
                { "Structures", random.Next(500, 2000) }
            }
        };
    }

    private List<InstanceDataCache.LogEntry> GenerateMockLogs(string instanceId)
    {
        var random = new Random(instanceId.GetHashCode() + DateTime.UtcNow.Second);
        var logCount = random.Next(3, 8);

        var messages = new[]
        {
            "Player joined the server",
            "Player disconnected",
            "Autosave completed successfully",
            "Server tick rate: 30 TPS",
            "Memory usage: stable",
            "New player spawned in base",
            "Pal captured by player",
            "Base raid started",
            "Boss Pal spawned in wild",
            "Weather changed to rainy"
        };

        var levels = new[] { "INFO", "INFO", "INFO", "WARN", "ERROR" };

        return Enumerable.Range(0, logCount)
            .Select(i => new InstanceDataCache.LogEntry
            {
                Timestamp = DateTime.UtcNow.AddSeconds(-random.Next(1, 60)),
                Level = levels[random.Next(levels.Length)],
                Message = messages[random.Next(messages.Length)]
            })
            .OrderBy(x => x.Timestamp)
            .ToList();
    }
}
