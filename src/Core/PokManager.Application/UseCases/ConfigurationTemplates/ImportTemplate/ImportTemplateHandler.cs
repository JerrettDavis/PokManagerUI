using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ImportTemplate;

/// <summary>
/// Handler for importing configuration templates from JSON files.
/// </summary>
public class ImportTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly IAuditSink _auditSink;
    private readonly IClock _clock;

    public ImportTemplateHandler(
        IConfigurationTemplateStore templateStore,
        IAuditSink auditSink,
        IClock clock)
    {
        _templateStore = templateStore;
        _auditSink = auditSink;
        _clock = clock;
    }

    public async Task<Result<ImportTemplateResponse>> Handle(
        ImportTemplateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.TemplateData == null)
            return Result.Failure<ImportTemplateResponse>("Template data is required");

        // Import template from stream
        var importResult = await _templateStore.ImportTemplateAsync(request.TemplateData, cancellationToken);
        if (importResult.IsFailure)
            return Result.Failure<ImportTemplateResponse>(importResult.Error);

        var templateId = importResult.Value;

        // Get imported template details
        var templateResult = await _templateStore.GetTemplateAsync(templateId, cancellationToken);
        if (templateResult.IsFailure)
            return Result.Failure<ImportTemplateResponse>(templateResult.Error);

        var template = templateResult.Value;

        // Create audit event
        await _auditSink.EmitAsync(new AuditEvent(
            Guid.NewGuid(),
            "",
            "TemplateImported",
            "System",
            _clock.UtcNow,
            "Success",
            null,
            new Dictionary<string, string>
            {
                ["TemplateId"] = templateId,
                ["TemplateName"] = template.Name,
                ["Category"] = template.Category,
                ["CorrelationId"] = request.CorrelationId
            },
            null
        ), cancellationToken);

        return Result<ImportTemplateResponse>.Success(new ImportTemplateResponse(
            templateId,
            template.Name,
            _clock.UtcNow
        ));
    }
}
