using FluentValidation;

namespace PokManager.Application.UseCases.InstanceQuery;

/// <summary>
/// Validator for GetInstanceStatusRequest.
/// </summary>
public class GetInstanceStatusRequestValidator : AbstractValidator<GetInstanceStatusRequest>
{
    public GetInstanceStatusRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");
    }
}
