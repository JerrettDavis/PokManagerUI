using System.Text.RegularExpressions;
using PokManager.Domain.Common;

namespace PokManager.Domain.ValueObjects;

public sealed class ServerPassword : IEquatable<ServerPassword>
{
    private static readonly Regex ValidationPattern = new(@"^[a-zA-Z0-9]+$", RegexOptions.Compiled);
    private const int MinLength = 4;
    private const int MaxLength = 64;

    public string Value { get; }

    private ServerPassword(string value)
    {
        Value = value;
    }

    public static Result<ServerPassword> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<ServerPassword>.Failure("ServerPassword cannot be empty or whitespace.");
        }

        if (value.Length < MinLength)
        {
            return Result<ServerPassword>.Failure($"ServerPassword must be at least {MinLength} characters.");
        }

        if (value.Length > MaxLength)
        {
            return Result<ServerPassword>.Failure($"ServerPassword cannot exceed {MaxLength} characters.");
        }

        if (!ValidationPattern.IsMatch(value))
        {
            return Result<ServerPassword>.Failure("ServerPassword must contain only alphanumeric characters.");
        }

        return Result<ServerPassword>.Success(new ServerPassword(value));
    }

    public bool Equals(ServerPassword? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is ServerPassword other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }

    public static bool operator ==(ServerPassword? left, ServerPassword? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ServerPassword? left, ServerPassword? right)
    {
        return !Equals(left, right);
    }
}
