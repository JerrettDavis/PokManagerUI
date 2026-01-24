using PokManager.Application.Ports;
using PokManager.Domain.Common;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;

/// <summary>
/// Handler for listing configuration templates with optional filtering.
/// </summary>
public class ListTemplatesHandler
{
    private readonly IConfigurationTemplateStore _templateStore;
    private readonly ListTemplatesValidator _validator;

    public ListTemplatesHandler(IConfigurationTemplateStore templateStore)
    {
        _templateStore = templateStore;
        _validator = new ListTemplatesValidator();
    }

    public async Task<Result<ListTemplatesResponse>> Handle(
        ListTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        // Validate request
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<ListTemplatesResponse>(
                $"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        // Retrieve templates from store with filters
        var templatesResult = await _templateStore.ListTemplatesAsync(
            request.Category,
            request.Type,
            cancellationToken);

        if (templatesResult.IsFailure)
            return Result.Failure<ListTemplatesResponse>(templatesResult.Error);

        var templates = templatesResult.Value;

        // Apply map filter if provided
        if (!string.IsNullOrWhiteSpace(request.MapFilter))
        {
            templates = templates
                .Where(t => string.IsNullOrWhiteSpace(string.Join(",", t.MapCompatibility)) ||
                           t.MapCompatibility.Contains(request.MapFilter, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        // Map to summary DTOs
        var summaries = templates.Select(t => new TemplateSummaryDto(
            t.TemplateId,
            t.Name,
            t.Description,
            t.Type,
            t.Category,
            t.Difficulty,
            t.IsPartial,
            t.TimesUsed,
            t.CreatedAt,
            t.Tags
        )).ToList();

        return Result<ListTemplatesResponse>.Success(new ListTemplatesResponse(summaries));
    }
}
