using FluentValidation;

namespace PokManager.Application.UseCases.InstanceLifecycle.StopInstance;

public class StopInstanceRequestValidator : AbstractValidator<StopInstanceRequest>
{
    public StopInstanceRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x.TimeoutSeconds)
            .GreaterThan(0).WithMessage("Timeout must be greater than 0 seconds")
            .LessThanOrEqualTo(300).WithMessage("Timeout must not exceed 300 seconds");
    }
}
