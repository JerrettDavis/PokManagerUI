using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ExportTemplate;

/// <summary>
/// Handler for exporting configuration templates as JSON files.
/// </summary>
public class ExportTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;

    public ExportTemplateHandler(IConfigurationTemplateStore templateStore)
    {
        _templateStore = templateStore;
    }

    public async Task<Result<ExportTemplateResponse>> Handle(
        ExportTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateId))
            return Result.Failure<ExportTemplateResponse>("Template ID is required");

        // Get template metadata for filename
        var templateResult = await _templateStore.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (templateResult.IsFailure)
            return Result.Failure<ExportTemplateResponse>(templateResult.Error);

        var template = templateResult.Value;

        // Export template as JSON stream
        var exportResult = await _templateStore.ExportTemplateAsync(request.TemplateId, cancellationToken);
        if (exportResult.IsFailure)
            return Result.Failure<ExportTemplateResponse>(exportResult.Error);

        // Generate filename from template name (sanitized)
        var sanitizedName = string.Join("_", template.Name.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{sanitizedName}_template.json";

        return Result<ExportTemplateResponse>.Success(new ExportTemplateResponse(
            exportResult.Value,
            fileName
        ));
    }
}
