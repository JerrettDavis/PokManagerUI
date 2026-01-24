namespace PokManager.Domain.Common;

public class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of failed result");

    public string Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access error of successful result");

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(string error)
    {
        IsSuccess = false;
        _value = default;
        _error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public static class Result
{
    public static Result<Unit> Success() => Result<Unit>.Success(Unit.Value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

public readonly struct Unit
{
    public static readonly Unit Value = new();
}
