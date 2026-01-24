using System.Net.Http.Json;
using PokManager.Web.Models;
using PokManager.Domain.Enumerations;

namespace PokManager.Web.Services;

/// <summary>
/// API client for backup management operations.
/// </summary>
public class BackupApiClient
{
    private readonly HttpClient _httpClient;

    public BackupApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Gets all backups, optionally filtered by instance.
    /// </summary>
    public async Task<List<BackupViewModel>> GetBackupsAsync(string? instanceId = null, CancellationToken cancellationToken = default)
    {
        var url = instanceId == null
            ? "api/backups"
            : $"api/backups?instanceId={Uri.EscapeDataString(instanceId)}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListBackupsResponseDto>(cancellationToken);
        return result?.Backups?.Select(b => new BackupViewModel
        {
            BackupId = b.BackupId,
            InstanceId = b.InstanceId,
            CreatedAt = b.CreatedAt,
            SizeBytes = b.SizeBytes,
            CompressionFormat = Enum.TryParse<CompressionFormat>(b.CompressionFormat.ToString(), out var format) ? format : CompressionFormat.Unknown,
            IsAutomatic = b.IsAutomatic,
            Description = b.Description
        }).ToList() ?? new List<BackupViewModel>();
    }

    private record ListBackupsResponseDto(List<BackupDto> Backups);
    private record BackupDto(string BackupId, string InstanceId, DateTimeOffset CreatedAt, long SizeBytes, int CompressionFormat, bool IsAutomatic, string? Description);

    /// <summary>
    /// Gets a specific backup by ID.
    /// </summary>
    public async Task<BackupViewModel?> GetBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/backups/{Uri.EscapeDataString(backupId)}", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<BackupViewModel>(cancellationToken);
    }

    /// <summary>
    /// Creates a new backup for the specified instance.
    /// </summary>
    public async Task<BackupViewModel> CreateBackupAsync(CreateBackupViewModel model, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/backups", model, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<BackupViewModel>(cancellationToken)
               ?? throw new InvalidOperationException("Failed to create backup");
    }

    /// <summary>
    /// Restores a backup to its instance.
    /// </summary>
    public async Task RestoreBackupAsync(RestoreBackupViewModel model, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"api/backups/{Uri.EscapeDataString(model.BackupId)}/restore", model, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Downloads a backup file.
    /// </summary>
    public async Task<Stream> DownloadBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/backups/{Uri.EscapeDataString(backupId)}/download", cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes a backup.
    /// </summary>
    public async Task DeleteBackupAsync(string backupId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"api/backups/{Uri.EscapeDataString(backupId)}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Uploads a backup file for a specific instance.
    /// </summary>
    public async Task<UploadBackupResponseDto> UploadBackupAsync(
        string instanceId,
        Stream fileStream,
        string fileName,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(fileStream);

        content.Add(streamContent, "file", fileName);
        content.Add(new StringContent(instanceId), "instanceId");

        if (!string.IsNullOrWhiteSpace(description))
        {
            content.Add(new StringContent(description), "description");
        }

        var response = await _httpClient.PostAsync("api/backups/upload", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<UploadBackupResponseDto>(cancellationToken)
               ?? throw new InvalidOperationException("Failed to upload backup");
    }

    public record UploadBackupResponseDto(
        bool Success,
        string BackupId,
        string InstanceId,
        string FilePath,
        long SizeInBytes,
        bool Restored,
        string? Message);

    /// <summary>
    /// Gets all server instances for dropdown selection.
    /// </summary>
    public async Task<List<InstanceViewModel>> GetInstancesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/instances", cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ListInstancesResponseDto>(cancellationToken);

        return result?.Instances?.Select(i => new InstanceViewModel
        {
            Id = i.Id,
            Name = i.Id, // Use ID as name for now since InstanceSummaryDto doesn't have Name
            Status = MapState(i.State),
            Health = MapHealth(i.Health),
            StartedAt = i.LastStartedAt?.DateTime,
            ContainerName = i.ContainerId ?? string.Empty
        }).ToList() ?? new List<InstanceViewModel>();
    }

    private static InstanceStatus MapState(int state) => state switch
    {
        3 => InstanceStatus.Running,    // Running
        5 => InstanceStatus.Stopped,    // Stopped
        2 => InstanceStatus.Starting,   // Starting
        4 => InstanceStatus.Stopping,   // Stopping
        7 => InstanceStatus.Error,      // Failed
        _ => InstanceStatus.Unknown
    };

    private static InstanceHealth MapHealth(int health) => health switch
    {
        1 => InstanceHealth.Healthy,    // Healthy
        2 => InstanceHealth.Degraded,   // Degraded
        3 => InstanceHealth.Unhealthy,  // Unhealthy
        _ => InstanceHealth.Unknown
    };

    private record ListInstancesResponseDto(List<InstanceSummaryDtoWeb> Instances);
    private record InstanceSummaryDtoWeb(string Id, int State, int Health, DateTimeOffset? LastStartedAt, string? ContainerId);
}
