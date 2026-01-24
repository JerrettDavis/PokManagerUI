using System.Net.Http.Json;

namespace PokManager.Web.Services;

/// <summary>
/// API client for log management operations.
/// </summary>
public class LogApiClient
{
    private readonly HttpClient _httpClient;

    public LogApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets the current game log for an instance.
    /// </summary>
    public async Task<GameLogResponse?> GetGameLogAsync(string instanceId, int? tail = null, CancellationToken cancellationToken = default)
    {
        var url = tail.HasValue
            ? $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/game?tail={tail.Value}"
            : $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/game";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GameLogResponse>(cancellationToken);
    }

    /// <summary>
    /// Lists all game logs (current and historical) for an instance.
    /// </summary>
    public async Task<GameLogListResponse?> ListGameLogsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/instances/{Uri.EscapeDataString(instanceId)}/logs/game/list", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<GameLogListResponse>(cancellationToken);
    }

    /// <summary>
    /// Downloads a specific game log file.
    /// </summary>
    public async Task<Stream?> DownloadGameLogAsync(string instanceId, string fileName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/game/download/{Uri.EscapeDataString(fileName)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current API log for an instance.
    /// </summary>
    public async Task<ApiLogResponse?> GetApiLogAsync(string instanceId, int? tail = null, CancellationToken cancellationToken = default)
    {
        var url = tail.HasValue
            ? $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/api?tail={tail.Value}"
            : $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/api";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ApiLogResponse>(cancellationToken);
    }

    /// <summary>
    /// Lists all API logs for an instance.
    /// </summary>
    public async Task<ApiLogListResponse?> ListApiLogsAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/instances/{Uri.EscapeDataString(instanceId)}/logs/api/list", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ApiLogListResponse>(cancellationToken);
    }

    /// <summary>
    /// Downloads a specific API log file.
    /// </summary>
    public async Task<Stream?> DownloadApiLogAsync(string instanceId, string fileName, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/instances/{Uri.EscapeDataString(instanceId)}/logs/api/download/{Uri.EscapeDataString(fileName)}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}

public record GameLogResponse(string InstanceId, string LogType, string Content, int LineCount);
public record ApiLogResponse(string InstanceId, string LogType, string FileName, string Content, int LineCount);
public record GameLogListResponse(string InstanceId, List<LogFileInfo> Logs);
public record ApiLogListResponse(string InstanceId, List<LogFileInfo> Logs);
public record LogFileInfo(string FileName, long SizeBytes, DateTime LastModified, bool? IsCurrent = null);
