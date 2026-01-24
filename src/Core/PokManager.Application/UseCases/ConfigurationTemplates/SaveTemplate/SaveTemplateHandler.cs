using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

/// <summary>
/// Handler for saving a new configuration template.
/// </summary>
public class SaveTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly IAuditSink _auditSink;
    private readonly IClock _clock;
    private readonly SaveTemplateValidator _validator;

    public SaveTemplateHandler(
        IConfigurationTemplateStore templateStore,
        IAuditSink auditSink,
        IClock clock)
    {
        _templateStore = templateStore;
        _auditSink = auditSink;
        _clock = clock;
        _validator = new SaveTemplateValidator();
    }

    public async Task<Result<SaveTemplateResponse>> Handle(
        SaveTemplateRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<SaveTemplateResponse>(
                $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        // Determine if this is a partial template
        var isPartial = request.IncludedSettings != null && request.IncludedSettings.Length > 0;
        var includedSettings = isPartial ? request.IncludedSettings! : Array.Empty<string>();

        // Create template info
        var now = _clock.UtcNow;
        var templateId = Guid.NewGuid().ToString();

        var templateInfo = new ConfigurationTemplateInfo(
            TemplateId: templateId,
            Name: request.Name,
            Description: request.Description,
            Type: 1, // UserCreated
            IsPartial: isPartial,
            Category: request.Category,
            Difficulty: request.Difficulty,
            MapCompatibility: request.MapCompatibility ?? Array.Empty<string>(),
            Tags: request.Tags ?? Array.Empty<string>(),
            ConfigurationData: request.ConfigurationSettings,
            IncludedSettings: includedSettings,
            CreatedAt: now,
            UpdatedAt: null,
            Author: request.Author,
            TimesUsed: 0
        );

        // Save template to store
        var saveResult = await _templateStore.SaveTemplateAsync(templateInfo, cancellationToken);
        if (saveResult.IsFailure)
            return Result.Failure<SaveTemplateResponse>(saveResult.Error);

        // Create audit event
        await _auditSink.EmitAsync(new AuditEvent(
            Guid.NewGuid(),
            "",
            "TemplateCreated",
            "System",
            now,
            "Success",
            null,
            new Dictionary<string, string>
            {
                ["TemplateId"] = templateId,
                ["TemplateName"] = request.Name,
                ["Category"] = request.Category,
                ["IsPartial"] = isPartial.ToString(),
                ["Author"] = request.Author,
                ["CorrelationId"] = request.CorrelationId
            },
            null
        ), cancellationToken);

        return Result<SaveTemplateResponse>.Success(new SaveTemplateResponse(
            templateId,
            request.Name,
            now
        ));
    }
}
