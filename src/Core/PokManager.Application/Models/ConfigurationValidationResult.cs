namespace PokManager.Application.Models;

/// <summary>
/// Represents the result of validating server configuration.
/// </summary>
/// <param name="IsValid">Whether the configuration is valid.</param>
/// <param name="Errors">List of validation errors, if any.</param>
/// <param name="Warnings">List of validation warnings, if any.</param>
/// <param name="ValidatedAt">When the validation was performed.</param>
public record ConfigurationValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    DateTimeOffset ValidatedAt
);
