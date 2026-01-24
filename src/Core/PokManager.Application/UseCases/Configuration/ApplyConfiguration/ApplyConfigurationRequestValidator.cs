using FluentValidation;

namespace PokManager.Application.UseCases.Configuration.ApplyConfiguration;

public class ApplyConfigurationRequestValidator : AbstractValidator<ApplyConfigurationRequest>
{
    public ApplyConfigurationRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x.ConfigurationSettings)
            .NotNull().WithMessage("Configuration settings cannot be null")
            .NotEmpty().WithMessage("Configuration settings cannot be empty");

        RuleForEach(x => x.ConfigurationSettings.Keys)
            .NotEmpty().WithMessage("Configuration setting key cannot be empty");

        RuleForEach(x => x.ConfigurationSettings.Values)
            .NotNull().WithMessage("Configuration setting value cannot be null");
    }
}
