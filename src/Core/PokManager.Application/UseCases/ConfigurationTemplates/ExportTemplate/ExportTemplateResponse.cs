namespace PokManager.Application.UseCases.ConfigurationTemplates.ExportTemplate;

/// <summary>
/// Response containing the exported template stream.
/// </summary>
public record ExportTemplateResponse(
    Stream TemplateData,
    string FileName
);
