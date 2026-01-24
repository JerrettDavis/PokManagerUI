using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed record OperationId
{
    public Guid Value { get; }

    private OperationId(Guid value)
    {
        Value = value;
    }

    public static OperationId New() => new(Guid.NewGuid());

    public static Result<OperationId> Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result.Failure<OperationId>("OperationId cannot be empty");
        }

        return Result<OperationId>.Success(new OperationId(value));
    }

    public static Result<OperationId> Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<OperationId>("OperationId string cannot be empty");
        }

        if (!Guid.TryParse(value, out var guid))
        {
            return Result.Failure<OperationId>($"Invalid OperationId format: {value}");
        }

        return Create(guid);
    }

    public override string ToString() => Value.ToString();
}
