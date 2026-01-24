using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed record CorrelationId
{
    public Guid Value { get; }

    private CorrelationId(Guid value)
    {
        Value = value;
    }

    public static CorrelationId New() => new(Guid.NewGuid());

    public static Result<CorrelationId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result.Failure<CorrelationId>("CorrelationId cannot be empty");
        }

        return Result<CorrelationId>.Success(new CorrelationId(value));
    }

    public static Result<CorrelationId> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CorrelationId>("CorrelationId string cannot be empty");
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<CorrelationId>($"Invalid CorrelationId format: {value}");
        }

        return Create(guid);
    }

    public override string ToString() => Value.ToString();
}
