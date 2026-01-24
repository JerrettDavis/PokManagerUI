using PokManager.Domain.Common;

namespace PokManager.Application.Common;

public static class ResultExtensions
{
    /// <summary>
    /// Bind: transforms Result<T> -> Result<U> using a function that returns Result<U>
    /// </summary>
    public static Result<TNew> Bind<T, TNew>(
        this Result<T> result,
        Func<T, Result<TNew>> func)
    {
        return result.IsSuccess
            ? func(result.Value)
            : Result.Failure<TNew>(result.Error);
    }

    /// <summary>
    /// BindAsync: async version of Bind
    /// </summary>
    public static async Task<Result<TNew>> BindAsync<T, TNew>(
        this Result<T> result,
        Func<T, Task<Result<TNew>>> func)
    {
        return result.IsSuccess
            ? await func(result.Value)
            : Result.Failure<TNew>(result.Error);
    }

    /// <summary>
    /// Map: transforms Result<T> -> Result<U> using a function that returns U (not Result<U>)
    /// </summary>
    public static Result<TNew> Map<T, TNew>(
        this Result<T> result,
        Func<T, TNew> func)
    {
        return result.IsSuccess
            ? Result<TNew>.Success(func(result.Value))
            : Result.Failure<TNew>(result.Error);
    }

    /// <summary>
    /// Tap: execute side effect if result is success, pass through the result unchanged
    /// </summary>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// TapAsync: async version of Tap
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess)
            await action(result.Value);
        return result;
    }
}
