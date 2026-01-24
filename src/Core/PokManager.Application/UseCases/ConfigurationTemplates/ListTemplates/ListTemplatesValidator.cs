using FluentValidation;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ListTemplates;

/// <summary>
/// Validator for ListTemplatesRequest.
/// </summary>
public class ListTemplatesValidator : AbstractValidator<ListTemplatesRequest>
{
    public ListTemplatesValidator()
    {
        RuleFor(x => x.Type)
            .Must(type => !type.HasValue || type.Value == 0 || type.Value == 1)
            .WithMessage("Type must be null, 0 (Preset), or 1 (UserCreated)");
    }
}
