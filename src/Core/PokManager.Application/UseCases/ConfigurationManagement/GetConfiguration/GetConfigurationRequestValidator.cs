using FluentValidation;

namespace PokManager.Application.UseCases.ConfigurationManagement.GetConfiguration;

/// <summary>
/// Validator for GetConfigurationRequest.
/// </summary>
public class GetConfigurationRequestValidator : AbstractValidator<GetConfigurationRequest>
{
    public GetConfigurationRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");
    }
}
