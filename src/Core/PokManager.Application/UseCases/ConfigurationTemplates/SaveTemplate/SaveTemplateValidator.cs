using FluentValidation;

namespace PokManager.Application.UseCases.ConfigurationTemplates.SaveTemplate;

/// <summary>
/// Validator for SaveTemplateRequest.
/// </summary>
public class SaveTemplateValidator : AbstractValidator<SaveTemplateRequest>
{
    public SaveTemplateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Template name is required")
            .MaximumLength(200).WithMessage("Template name must not exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(50).WithMessage("Category must not exceed 50 characters");

        RuleFor(x => x.Difficulty)
            .MaximumLength(50).WithMessage("Difficulty must not exceed 50 characters");

        RuleFor(x => x.ConfigurationSettings)
            .NotNull().WithMessage("Configuration settings are required")
            .Must(settings => settings.Count > 0).WithMessage("Configuration settings must contain at least one setting");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters");
    }
}
