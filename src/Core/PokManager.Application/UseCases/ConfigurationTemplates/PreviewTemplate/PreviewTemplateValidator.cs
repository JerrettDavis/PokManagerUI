using FluentValidation;

namespace PokManager.Application.UseCases.ConfigurationTemplates.PreviewTemplate;

/// <summary>
/// Validator for PreviewTemplateRequest.
/// </summary>
public class PreviewTemplateValidator : AbstractValidator<PreviewTemplateRequest>
{
    public PreviewTemplateValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Template ID is required");

        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID is required");
    }
}
