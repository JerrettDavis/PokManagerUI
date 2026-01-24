using FluentValidation;

namespace PokManager.Application.UseCases.BackupManagement.UploadBackup;

public class UploadBackupRequestValidator : AbstractValidator<UploadBackupRequest>
{
    public UploadBackupRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name cannot be empty")
            .Must(fn => fn.EndsWith(".gz") || fn.EndsWith(".tar.gz") || fn.EndsWith(".zip"))
            .WithMessage("File must be a compressed backup (.gz, .tar.gz, or .zip)");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream cannot be null");

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");
    }
}
