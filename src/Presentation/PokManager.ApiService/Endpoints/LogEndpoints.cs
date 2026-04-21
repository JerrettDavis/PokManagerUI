using Microsoft.AspNetCore.Mvc;

namespace PokManager.ApiService.Endpoints;

public static class LogEndpoints
{
    public static void MapLogEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/instances/{instanceId}/logs").WithTags("Logs");

        // GET /api/instances/{instanceId}/logs/game - Get current game log
        group.MapGet("/game", async (
            string instanceId,
            [FromQuery] int? tail,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                var logPath = $"/home/pokuser/asa_server/Instance_{instanceId}/Saved/Logs/ShooterGame.log";

                if (!File.Exists(logPath))
                {
                    return Results.NotFound(new { error = $"Game log not found for instance {instanceId}" });
                }

                var lines = await File.ReadAllLinesAsync(logPath, ct);

                if (tail.HasValue && tail.Value > 0)
                {
                    lines = lines.TakeLast(tail.Value).ToArray();
                }

                return Results.Ok(new
                {
                    instanceId,
                    logType = "game",
                    content = string.Join(Environment.NewLine, lines),
                    lineCount = lines.Length
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error reading game log: {ex.Message}");
            }
        })
        .WithName("GetGameLog")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs/game/list - List all game logs (current and historical)
        group.MapGet("/game/list", (
            string instanceId) =>
        {
            try
            {
                var logsDir = $"/home/pokuser/asa_server/Instance_{instanceId}/Saved/Logs";

                if (!Directory.Exists(logsDir))
                {
                    return Results.NotFound(new { error = $"Logs directory not found for instance {instanceId}" });
                }

                var logFiles = Directory.GetFiles(logsDir, "*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new
                    {
                        fileName = f.Name,
                        sizeBytes = f.Length,
                        lastModified = f.LastWriteTime,
                        isCurrent = f.Name == "ShooterGame.log"
                    })
                    .ToList();

                return Results.Ok(new { instanceId, logs = logFiles });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error listing game logs: {ex.Message}");
            }
        })
        .WithName("ListGameLogs")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs/game/download/{fileName} - Download a specific game log
        group.MapGet("/game/download/{fileName}", async (
            string instanceId,
            string fileName,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                // Sanitize filename to prevent directory traversal
                fileName = Path.GetFileName(fileName);

                var logPath = Path.Combine($"/home/pokuser/asa_server/Instance_{instanceId}/Saved/Logs", fileName);

                if (!File.Exists(logPath))
                {
                    return Results.NotFound(new { error = $"Log file {fileName} not found" });
                }

                var fileStream = File.OpenRead(logPath);
                return Results.File(fileStream, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error downloading log file: {ex.Message}");
            }
        })
        .WithName("DownloadGameLog")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs/api - Get current API log
        group.MapGet("/api", async (
            string instanceId,
            [FromQuery] int? tail,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                var logsDir = $"/home/pokuser/asa_server/Instance_{instanceId}/API_Logs";

                if (!Directory.Exists(logsDir))
                {
                    return Results.NotFound(new { error = $"API logs directory not found for instance {instanceId}" });
                }

                // Get the most recent API log file
                var latestLog = Directory.GetFiles(logsDir, "ArkApi_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .FirstOrDefault();

                if (latestLog == null)
                {
                    return Results.NotFound(new { error = $"No API logs found for instance {instanceId}" });
                }

                var lines = await File.ReadAllLinesAsync(latestLog.FullName, ct);

                if (tail.HasValue && tail.Value > 0)
                {
                    lines = lines.TakeLast(tail.Value).ToArray();
                }

                return Results.Ok(new
                {
                    instanceId,
                    logType = "api",
                    fileName = latestLog.Name,
                    content = string.Join(Environment.NewLine, lines),
                    lineCount = lines.Length
                });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error reading API log: {ex.Message}");
            }
        })
        .WithName("GetApiLog")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs/api/list - List all API logs
        group.MapGet("/api/list", (
            string instanceId) =>
        {
            try
            {
                var logsDir = $"/home/pokuser/asa_server/Instance_{instanceId}/API_Logs";

                if (!Directory.Exists(logsDir))
                {
                    return Results.NotFound(new { error = $"API logs directory not found for instance {instanceId}" });
                }

                var logFiles = Directory.GetFiles(logsDir, "ArkApi_*.log*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => new
                    {
                        fileName = f.Name,
                        sizeBytes = f.Length,
                        lastModified = f.LastWriteTime
                    })
                    .ToList();

                return Results.Ok(new { instanceId, logs = logFiles });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error listing API logs: {ex.Message}");
            }
        })
        .WithName("ListApiLogs")
        .WithOpenApi();

        // GET /api/instances/{instanceId}/logs/api/download/{fileName} - Download a specific API log
        group.MapGet("/api/download/{fileName}", async (
            string instanceId,
            string fileName,
            HttpContext context,
            CancellationToken ct) =>
        {
            try
            {
                // Sanitize filename to prevent directory traversal
                fileName = Path.GetFileName(fileName);

                var logPath = Path.Combine($"/home/pokuser/asa_server/Instance_{instanceId}/API_Logs", fileName);

                if (!File.Exists(logPath))
                {
                    return Results.NotFound(new { error = $"Log file {fileName} not found" });
                }

                var fileStream = File.OpenRead(logPath);
                return Results.File(fileStream, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error downloading log file: {ex.Message}");
            }
        })
        .WithName("DownloadApiLog")
        .WithOpenApi();
    }
}
