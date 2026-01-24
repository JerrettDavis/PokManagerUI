using FluentValidation;

namespace PokManager.Application.UseCases.BackupManagement.CreateBackup;

public class CreateBackupRequestValidator : AbstractValidator<CreateBackupRequest>
{
    public CreateBackupRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x.Options!.Description)
            .MaximumLength(256).WithMessage("Description must be maximum 256 characters")
            .When(x => x.Options != null && !string.IsNullOrWhiteSpace(x.Options.Description));
    }
}
