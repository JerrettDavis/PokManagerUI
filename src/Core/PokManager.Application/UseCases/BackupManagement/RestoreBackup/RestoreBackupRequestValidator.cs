using FluentValidation;

namespace PokManager.Application.UseCases.BackupManagement.RestoreBackup;

public class RestoreBackupRequestValidator : AbstractValidator<RestoreBackupRequest>
{
    public RestoreBackupRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.BackupId)
            .NotEmpty().WithMessage("Backup ID cannot be empty");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x.Confirmed)
            .Equal(true).WithMessage("Restore operation must be confirmed for safety")
            .WithName("Confirmed");
    }
}
