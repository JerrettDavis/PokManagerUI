using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed class SessionName : IEquatable<SessionName>
{
    private const int MaxLength = 128;

    public string Value { get; }

    private SessionName(string value)
    {
        Value = value;
    }

    public static Result<SessionName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<SessionName>.Failure("SessionName cannot be empty or whitespace.");
        }

        if (value.Length > MaxLength)
        {
            return Result<SessionName>.Failure($"SessionName cannot exceed {MaxLength} characters.");
        }

        return Result<SessionName>.Success(new SessionName(value));
    }

    public bool Equals(SessionName? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SessionName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(SessionName? left, SessionName? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(SessionName? left, SessionName? right)
    {
        return !Equals(left, right);
    }
}
