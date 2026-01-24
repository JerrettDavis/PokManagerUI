using FluentValidation;

namespace PokManager.Application.UseCases.InstanceLifecycle.RestartInstance;

/// <summary>
/// Validator for RestartInstanceRequest.
/// </summary>
public class RestartInstanceRequestValidator : AbstractValidator<RestartInstanceRequest>
{
    public RestartInstanceRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x.GracePeriodSeconds)
            .GreaterThanOrEqualTo(0).WithMessage("Grace period must be greater than or equal to 0 seconds")
            .LessThanOrEqualTo(300).WithMessage("Grace period must not exceed 300 seconds");
    }
}
