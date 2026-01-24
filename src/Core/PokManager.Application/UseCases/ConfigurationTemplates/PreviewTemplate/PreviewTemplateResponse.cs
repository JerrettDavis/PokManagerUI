using PokManager.Application.Models;

namespace PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;

/// <summary>
/// Response containing preview of template changes.
/// </summary>
public record PreviewTemplateResponse(
    string TemplateId,
    string TemplateName,
    string InstanceId,
    IReadOnlyList<SettingChange> Changes,
    bool IsCompatible,
    string[] Warnings
);
