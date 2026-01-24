using Microsoft.AspNetCore.Mvc;
using PokManager.Application.UseCases.BackupManagement.CreateBackup;
using PokManager.Application.UseCases.BackupManagement.ListBackups;
using PokManager.Application.UseCases.BackupManagement.RestoreBackup;
using PokManager.Application.UseCases.BackupManagement.UploadBackup;
using PokManager.Application.Caching;
using PokManager.Application.Ports;
using PokManager.Application.BackgroundWorkers;
using PokManager.Application.Configuration;

namespace PokManager.ApiService.Endpoints;

/// <summary>
/// DTO for restore backup request.
/// </summary>
public record RestoreBackupRequestDto(
    string InstanceId,
    bool Confirmed = false,
    bool CreateSafetyBackup = true
);

public static class BackupEndpoints
{
    public static void MapBackupEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/backups").WithTags("Backups");

        // GET /api/backups - List all backups (cache-first)
        group.MapGet("/", async (
            [FromServices] ICacheService cacheService,
            [FromServices] ListBackupsHandler handler,
            [FromServices] CacheConfiguration cacheConfig,
            string? instanceId,
            CancellationToken ct) =>
        {
            var effectiveInstanceId = instanceId ?? string.Empty;

            // Try cache first if instance ID is specified
            if (!string.IsNullOrEmpty(effectiveInstanceId))
            {
                var cached = await cacheService.GetAsync<ListBackupsResponse>(
                    CacheKeys.BackupList(effectiveInstanceId),
                    ct);

                if (cached != null)
                {
                    return Results.Ok(cached);
                }
            }

            // Cache miss or no instance filter - fetch from handler
            var result = await handler.Handle(
                new ListBackupsRequest(effectiveInstanceId, Guid.NewGuid().ToString()),
                ct);

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error });
            }

            // Cache the result if instance ID is specified
            if (!string.IsNullOrEmpty(effectiveInstanceId))
            {
                await cacheService.SetAsync(
                    CacheKeys.BackupList(effectiveInstanceId),
                    result.Value,
                    cacheConfig.BackupListTtl,
                    ct);
            }

            return Results.Ok(result.Value);
        })
        .WithName("ListBackups")
        .WithOpenApi();

        // POST /api/backups/refresh - Trigger priority refresh of backup list
        group.MapPost("/refresh", async (
            [FromServices] IRefreshQueue refreshQueue,
            string? instanceId,
            CancellationToken ct) =>
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return Results.BadRequest(new { error = "instanceId parameter is required" });
            }

            await refreshQueue.EnqueueAsync(
                new RefreshRequest(RefreshType.BackupList, instanceId, Priority: true),
                ct);

            return Results.Accepted(null, new { message = $"Backup list refresh queued for {instanceId}" });
        })
        .WithName("RefreshBackupList")
        .WithOpenApi();

        // GET /api/backups/{backupId} - Get specific backup
        group.MapGet("/{backupId}", async (
            string backupId,
            [FromServices] ListBackupsHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new ListBackupsRequest(string.Empty, Guid.NewGuid().ToString()),
                ct);

            if (!result.IsSuccess)
                return Results.BadRequest(new { error = result.Error });

            var backup = result.Value.Backups.FirstOrDefault(b => b.BackupId == backupId);

            return backup != null
                ? Results.Ok(backup)
                : Results.NotFound(new { error = $"Backup '{backupId}' not found" });
        })
        .WithName("GetBackup")
        .WithOpenApi();

        // POST /api/backups - Create a new backup
        group.MapPost("/", async (
            CreateBackupRequest request,
            [FromServices] CreateBackupHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("CreateBackup")
        .WithOpenApi();

        // POST /api/backups/{backupId}/restore - Restore a backup
        group.MapPost("/{backupId}/restore", async (
            string backupId,
            [FromBody] RestoreBackupRequestDto dto,
            [FromServices] RestoreBackupHandler handler,
            CancellationToken ct) =>
        {
            var request = new RestoreBackupRequest(
                InstanceId: dto.InstanceId,
                BackupId: backupId,
                CorrelationId: Guid.NewGuid().ToString(),
                Confirmed: dto.Confirmed,
                CreateSafetyBackup: dto.CreateSafetyBackup
            );

            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("RestoreBackup")
        .WithOpenApi();

        // POST /api/backups/upload - Upload a backup file
        group.MapPost("/upload", async (
            [FromForm] IFormFile file,
            [FromForm] string instanceId,
            [FromForm] string? description,
            [FromServices] UploadBackupHandler handler,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "No file was uploaded" });
            }

            using var stream = file.OpenReadStream();
            var request = new UploadBackupRequest(
                InstanceId: instanceId,
                FileName: file.FileName,
                FileStream: stream,
                Description: description,
                CorrelationId: Guid.NewGuid().ToString(),
                RestoreImmediately: false
            );

            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("UploadBackup")
        .WithOpenApi()
        .DisableAntiforgery(); // Allow file uploads without antiforgery token

        // GET /api/backups/{backupId}/download - Download a backup
        group.MapGet("/{backupId}/download", async (
            string backupId,
            HttpContext context,
            CancellationToken ct) =>
        {
            // For now, return not implemented
            // TODO: Implement DownloadBackup functionality
            return Results.Problem(
                detail: "Backup download functionality is not yet implemented",
                statusCode: 501,
                title: "Not Implemented"
            );
        })
        .WithName("DownloadBackup")
        .WithOpenApi();

        // DELETE /api/backups/{backupId} - Delete a backup
        group.MapDelete("/{backupId}", async (
            string backupId,
            HttpContext context,
            CancellationToken ct) =>
        {
            // For now, return not implemented
            // TODO: Implement DeleteBackup use case
            return Results.Problem(
                detail: "Backup deletion functionality is not yet implemented",
                statusCode: 501,
                title: "Not Implemented"
            );
        })
        .WithName("DeleteBackup")
        .WithOpenApi();
    }
}
