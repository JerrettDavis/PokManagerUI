using Microsoft.AspNetCore.Mvc;
using PokManager.Application.Ports;
using PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.DeleteTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ExportTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ImportTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;
using PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;
using PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

namespace PokManager.ApiService.Endpoints;

public static class ConfigurationTemplateEndpoints
{
    public static void MapConfigurationTemplateEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/configuration-templates").WithTags("Configuration Templates");

        // GET /api/configuration-templates - List all templates
        group.MapGet("/", async (
            [FromServices] ListTemplatesHandler handler,
            string? category,
            int? type,
            string? mapFilter,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new ListTemplatesRequest(category, type, mapFilter, Guid.NewGuid().ToString()),
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ListConfigurationTemplates")
        .WithOpenApi();

        // GET /api/configuration-templates/{templateId} - Get specific template
        group.MapGet("/{templateId}", async (
            string templateId,
            [FromServices] IConfigurationTemplateStore store,
            CancellationToken ct) =>
        {
            var result = await store.GetTemplateAsync(templateId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        })
        .WithName("GetConfigurationTemplate")
        .WithOpenApi();

        // POST /api/configuration-templates - Create a new template
        group.MapPost("/", async (
            SaveTemplateRequest request,
            [FromServices] SaveTemplateHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("SaveConfigurationTemplate")
        .WithOpenApi();

        // DELETE /api/configuration-templates/{templateId} - Delete a template
        group.MapDelete("/{templateId}", async (
            string templateId,
            [FromServices] DeleteTemplateHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new DeleteTemplateRequest(templateId, Guid.NewGuid().ToString()),
                ct);

            return result.IsSuccess
                ? Results.Ok(new { message = "Template deleted successfully" })
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("DeleteConfigurationTemplate")
        .WithOpenApi();

        // POST /api/configuration-templates/{templateId}/apply - Apply template to instance
        group.MapPost("/{templateId}/apply", async (
            string templateId,
            [FromBody] ApplyTemplateRequestDto dto,
            [FromServices] ApplyTemplateHandler handler,
            CancellationToken ct) =>
        {
            var request = new ApplyTemplateRequest(
                templateId,
                dto.InstanceId,
                dto.CreateBackup,
                dto.RestartIfNeeded,
                Guid.NewGuid().ToString()
            );

            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ApplyConfigurationTemplate")
        .WithOpenApi();

        // POST /api/configuration-templates/{templateId}/preview - Preview template changes
        group.MapPost("/{templateId}/preview", async (
            string templateId,
            [FromBody] PreviewTemplateRequestDto dto,
            [FromServices] PreviewTemplateHandler handler,
            CancellationToken ct) =>
        {
            var request = new PreviewTemplateRequest(
                templateId,
                dto.InstanceId,
                Guid.NewGuid().ToString()
            );

            var result = await handler.Handle(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("PreviewConfigurationTemplate")
        .WithOpenApi();

        // GET /api/configuration-templates/{templateId}/export - Export template
        group.MapGet("/{templateId}/export", async (
            string templateId,
            [FromServices] ExportTemplateHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.Handle(
                new ExportTemplateRequest(templateId, Guid.NewGuid().ToString()),
                ct);

            if (result.IsFailure)
                return Results.BadRequest(new { error = result.Error });

            var exportResult = result.Value;
            return Results.File(
                exportResult.TemplateData,
                "application/json",
                exportResult.FileName
            );
        })
        .WithName("ExportConfigurationTemplate")
        .WithOpenApi();

        // POST /api/configuration-templates/import - Import template
        group.MapPost("/import", async (
            IFormFile file,
            [FromServices] ImportTemplateHandler handler,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
                return Results.BadRequest(new { error = "No file provided" });

            using var stream = file.OpenReadStream();
            var result = await handler.Handle(
                new ImportTemplateRequest(stream, Guid.NewGuid().ToString()),
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .WithName("ImportConfigurationTemplate")
        .WithOpenApi()
        .DisableAntiforgery(); // Allow file upload without antiforgery token
    }

    // DTOs for request bodies (since route parameters can't capture complex objects)
    public record ApplyTemplateRequestDto(
        string InstanceId,
        bool CreateBackup = true,
        bool RestartIfNeeded = true
    );

    public record PreviewTemplateRequestDto(
        string InstanceId
    );
}
