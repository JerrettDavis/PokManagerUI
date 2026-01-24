using PokManager.Domain.Common;

namespace PokManager.Application.Common;

/// <summary>
/// Enforces explicit confirmation for destructive operations.
/// </summary>
public static class ConfirmationGate
{
    public static Result<Unit> RequireConfirmation(bool confirmed, string operationName)
    {
        if (!confirmed)
        {
            return Result.Failure<Unit>(
                $"Operation '{operationName}' requires explicit confirmation. " +
                $"Set Confirmed = true to proceed.");
        }

        return Result.Success();
    }
}
