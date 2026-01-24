namespace TinyBDD;

/// <summary>
/// Simple TinyBDD-style test context for fluent Given-When-Then test syntax.
/// </summary>
public static class TestContext
{
    public static TestContextBuilder Run => new();
}

/// <summary>
/// Builder for fluent test context with Given-When-Then pattern.
/// </summary>
public class TestContextBuilder
{
    // Given with context
    public TestContextWithGiven<TContext> Given<TContext>(string description, Func<TContext> arrange)
    {
        return new TestContextWithGiven<TContext>(arrange);
    }

    // Given with void (setup only)
    public TestContextWithGivenVoid Given(string description, Action arrange)
    {
        return new TestContextWithGivenVoid(arrange);
    }
}

/// <summary>
/// Test context after Given step (with context).
/// </summary>
public class TestContextWithGiven<TContext>(Func<TContext> arrange)
{
    public TestContextWithWhen<TContext, TResult> When<TResult>(string description, Func<TContext, Task<TResult>> act)
    {
        return new TestContextWithWhen<TContext, TResult>(arrange, act);
    }

    public TestContextWithWhen<TContext, TResult> When<TResult>(string description, Func<TContext, TResult> act)
    {
        return new TestContextWithWhen<TContext, TResult>(arrange, ctx => Task.FromResult(act(ctx)));
    }
}

/// <summary>
/// Test context after Given step (without context).
/// </summary>
public class TestContextWithGivenVoid(Action arrange)
{
    public TestContextWithWhenVoid<TResult> When<TResult>(string description, Func<Task<TResult>> act)
    {
        return new TestContextWithWhenVoid<TResult>(arrange, act);
    }

    public TestContextWithWhenVoid<TResult> When<TResult>(string description, Func<TResult> act)
    {
        return new TestContextWithWhenVoid<TResult>(arrange, () => Task.FromResult(act()));
    }
}

/// <summary>
/// Test context after When step (with context).
/// </summary>
public class TestContextWithWhen<TContext, TResult>(Func<TContext> arrange, Func<TContext, Task<TResult>> act)
{
    public TestContextWithThen<TContext, TResult> Then(string description, Action<TResult> assert)
    {
        return new TestContextWithThen<TContext, TResult>(arrange, act, assert);
    }
}

/// <summary>
/// Test context after When step (without context).
/// </summary>
public class TestContextWithWhenVoid<TResult>(Action arrange, Func<Task<TResult>> act)
{
    public TestContextWithThenVoid<TResult> Then(string description, Action<TResult> assert)
    {
        return new TestContextWithThenVoid<TResult>(arrange, act, assert);
    }
}

/// <summary>
/// Test context after Then step (with context).
/// </summary>
public class TestContextWithThen<TContext, TResult>
{
    private readonly Func<TContext> _arrange;
    private readonly Func<TContext, Task<TResult>> _act;
    private readonly List<Action<TResult>> _assertions = new();

    public TestContextWithThen(Func<TContext> arrange, Func<TContext, Task<TResult>> act, Action<TResult> assert)
    {
        _arrange = arrange;
        _act = act;
        _assertions.Add(assert);
    }

    public TestContextWithThen<TContext, TResult> And(string description, Action<TResult> assert)
    {
        _assertions.Add(assert);
        return this;
    }

    public void Run()
    {
        // Arrange
        var context = _arrange();

        // Act
        var result = _act(context).GetAwaiter().GetResult();

        // Assert
        foreach (var assertion in _assertions)
        {
            assertion(result);
        }
    }
}

/// <summary>
/// Test context after Then step (without context).
/// </summary>
public class TestContextWithThenVoid<TResult>
{
    private readonly Action _arrange;
    private readonly Func<Task<TResult>> _act;
    private readonly List<Action<TResult>> _assertions = new();

    public TestContextWithThenVoid(Action arrange, Func<Task<TResult>> act, Action<TResult> assert)
    {
        _arrange = arrange;
        _act = act;
        _assertions.Add(assert);
    }

    public TestContextWithThenVoid<TResult> And(string description, Action<TResult> assert)
    {
        _assertions.Add(assert);
        return this;
    }

    public void Run()
    {
        // Arrange
        _arrange();

        // Act
        var result = _act().GetAwaiter().GetResult();

        // Assert
        foreach (var assertion in _assertions)
        {
            assertion(result);
        }
    }
}
