using FluentValidation;
using PokManager.Application.Models;
using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.DeleteTemplate;

/// <summary>
/// Handler for deleting user-created configuration templates.
/// Preset templates (Type=0) cannot be deleted.
/// </summary>
public class DeleteTemplateHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly IAuditSink _auditSink;
    private readonly IClock _clock;

    public DeleteTemplateHandler(
        IConfigurationTemplateStore templateStore,
        IAuditSink auditSink,
        IClock clock)
    {
        _templateStore = templateStore;
        _auditSink = auditSink;
        _clock = clock;
    }

    public async Task<Result<Unit>> Handle(
        DeleteTemplateRequest request,
        CancellationToken cancellationToken)
    {
        // Validate template ID
        if (string.IsNullOrWhiteSpace(request.TemplateId))
            return Result.Failure<Unit>("Template ID is required");

        // Get template to check if it exists and is user-created
        var templateResult = await _templateStore.GetTemplateAsync(request.TemplateId, cancellationToken);
        if (templateResult.IsFailure)
            return Result.Failure<Unit>(templateResult.Error);

        var template = templateResult.Value;

        // Prevent deletion of preset templates
        if (template.Type == 0) // Preset
        {
            return Result.Failure<Unit>("Cannot delete preset templates. Only user-created templates can be deleted.");
        }

        // Delete template
        var deleteResult = await _templateStore.DeleteTemplateAsync(request.TemplateId, cancellationToken);
        if (deleteResult.IsFailure)
            return deleteResult;

        // Create audit event
        await _auditSink.EmitAsync(new AuditEvent(
            Guid.NewGuid(),
            "",
            "TemplateDeleted",
            "System",
            _clock.UtcNow,
            "Success",
            null,
            new Dictionary<string, string>
            {
                ["TemplateId"] = template.TemplateId,
                ["TemplateName"] = template.Name,
                ["Category"] = template.Category,
                ["CorrelationId"] = request.CorrelationId
            },
            null
        ), cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
