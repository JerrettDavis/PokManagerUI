using FluentValidation;

namespace PokManager.Application.UseCases.ConfigurationTemplates.ApplyTemplate;

/// <summary>
/// Validator for ApplyTemplateRequest.
/// </summary>
public class ApplyTemplateValidator : AbstractValidator<ApplyTemplateRequest>
{
    public ApplyTemplateValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty().WithMessage("Template ID is required");

        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID is required");
    }
}
