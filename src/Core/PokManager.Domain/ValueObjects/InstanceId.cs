using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed class InstanceId : IEquatable<InstanceId>
{
    private static readonly Regex s_validationPattern = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private const int MaxLength = 64;

    public string Value { get; }

    private InstanceId(string value)
    {
        Value = value;
    }

    public static Result<InstanceId> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<InstanceId>.Failure("InstanceId cannot be empty or whitespace.");
        }

        if (value.Length > MaxLength)
        {
            return Result<InstanceId>.Failure($"InstanceId cannot exceed {MaxLength} characters.");
        }

        if (!s_validationPattern.IsMatch(value))
        {
            return Result<InstanceId>.Failure("InstanceId must contain only alphanumeric characters, hyphens, and underscores.");
        }

        return Result<InstanceId>.Success(new InstanceId(value));
    }

    public bool Equals(InstanceId? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is InstanceId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(InstanceId? left, InstanceId? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(InstanceId? left, InstanceId? right)
    {
        return !Equals(left, right);
    }
}
