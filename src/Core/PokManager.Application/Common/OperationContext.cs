namespace PokManager.Application.Common;

/// <summary>
/// Contains contextual information about an operation execution.
/// </summary>
public record OperationContext(
    string CorrelationId,
    string? UserId,
    DateTimeOffset Timestamp)
{
    public static OperationContext Create(string correlationId, string? userId = null)
    {
        return new OperationContext(
            correlationId,
            userId,
            DateTimeOffset.UtcNow);
    }
}
