using FluentValidation;

namespace PokManager.Application.UseCases.InstanceLifecycle.CreateInstance;

public class CreateInstanceRequestValidator : AbstractValidator<CreateInstanceRequest>
{
    private static readonly string[] s_validMapNames = new[]
    {
        "TheIsland",
        "TheCenter",
        "ScorchedEarth",
        "Ragnarok",
        "Aberration",
        "Extinction",
        "Valguero",
        "Genesis",
        "CrystalIsles",
        "Genesis2",
        "LostIsland",
        "Fjordur",
        "ASA_TheIsland"
    };

    public CreateInstanceRequestValidator()
    {
        RuleFor(x => x.InstanceId)
            .NotEmpty().WithMessage("Instance ID cannot be empty")
            .MaximumLength(64).WithMessage("Instance ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Instance ID must contain only alphanumeric characters, hyphens, and underscores");

        RuleFor(x => x.SessionName)
            .NotEmpty().WithMessage("Session name cannot be empty")
            .MaximumLength(128).WithMessage("Session name must be maximum 128 characters");

        RuleFor(x => x.MapName)
            .NotEmpty().WithMessage("Map name cannot be empty")
            .Must(BeAValidMapName).WithMessage($"Map name must be one of: {string.Join(", ", s_validMapNames)}");

        RuleFor(x => x.MaxPlayers)
            .InclusiveBetween(1, 127).WithMessage("Max players must be between 1 and 127");

        RuleFor(x => x.ServerAdminPassword)
            .NotEmpty().WithMessage("Server admin password cannot be empty")
            .Length(4, 64).WithMessage("Server admin password must be between 4 and 64 characters")
            .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Server admin password must contain only alphanumeric characters");

        RuleFor(x => x.ServerPassword)
            .Length(4, 64).WithMessage("Server password must be between 4 and 64 characters")
            .Matches(@"^[a-zA-Z0-9]+$").WithMessage("Server password must contain only alphanumeric characters")
            .When(x => !string.IsNullOrWhiteSpace(x.ServerPassword));

        RuleFor(x => x.GamePort)
            .InclusiveBetween(1024, 65535).WithMessage("Game port must be between 1024 and 65535");

        RuleFor(x => x.RconPort)
            .InclusiveBetween(1024, 65535).WithMessage("RCON port must be between 1024 and 65535");

        RuleFor(x => x.ClusterId)
            .MaximumLength(64).WithMessage("Cluster ID must be maximum 64 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Cluster ID must contain only alphanumeric characters, hyphens, and underscores")
            .When(x => !string.IsNullOrWhiteSpace(x.ClusterId));

        RuleFor(x => x.CorrelationId)
            .NotEmpty().WithMessage("Correlation ID cannot be empty");

        RuleFor(x => x)
            .Must(x => x.GamePort != x.RconPort)
            .WithMessage("Game port and RCON port must be different")
            .WithName("Ports");
    }

    private bool BeAValidMapName(string mapName)
    {
        return s_validMapNames.Contains(mapName, StringComparer.OrdinalIgnoreCase);
    }
}
