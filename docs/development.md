# Development Guide

This guide covers the development workflow, coding standards, and best practices for contributing to PokManagerApi.

## Table of Contents

- [Development Workflow](#development-workflow)
- [TDD with TinyBDD](#tdd-with-tinybdd)
- [Coding Standards](#coding-standards)
- [Testing Guidelines](#testing-guidelines)
- [Build System](#build-system)
- [IDE Setup](#ide-setup)
- [Debugging Tips](#debugging-tips)
- [Git Workflow](#git-workflow)
- [Code Review](#code-review)

## Development Workflow

### The TDD Cycle

PokManagerApi follows a strict **Test-Driven Development (TDD)** approach using TinyBDD:

```
1. RED    → Write a failing test
2. GREEN  → Write minimal code to pass
3. REFACTOR → Improve code while keeping tests green
4. REPEAT → Continue for next requirement
```

### Feature Development Flow

1. **Understand the Requirement**
   - Review feature specification
   - Identify affected layers (Domain, Application, Infrastructure)
   - Plan test scenarios

2. **Write Tests First**
   - Start with TinyBDD tests describing behavior
   - Cover happy path and edge cases
   - Include error scenarios

3. **Implement Minimal Code**
   - Write just enough code to pass tests
   - Don't over-engineer
   - Keep it simple

4. **Refactor**
   - Clean up code
   - Extract methods/classes
   - Improve naming
   - Ensure tests still pass

5. **Integration**
   - Test integration with other layers
   - Update documentation
   - Prepare for code review

### Example: Adding a New Feature

**Feature**: Add "Restart All Instances" functionality

#### Step 1: Write Application Test

```csharp
// tests/PokManager.Application.Tests/UseCases/Instances/RestartAllInstancesHandlerTests.cs

public class When_restarting_all_instances : Feature
{
    private IRestartAllInstancesHandler _handler;
    private IPokManagerClient _mockClient;
    private List<string> _instances;

    public When_restarting_all_instances()
    {
        Given.A_handler_with_mocked_dependencies();
        And.Multiple_instances_exist();
    }

    [Fact]
    public void Should_restart_all_instances_successfully()
    {
        When.Restarting_all_instances();
        Then.All_instances_should_be_restarted();
        And.Operation_should_succeed();
    }

    [Fact]
    public void Should_continue_if_one_instance_fails()
    {
        Given.One_instance_will_fail_to_restart();
        When.Restarting_all_instances();
        Then.Other_instances_should_still_be_restarted();
        And.Partial_failure_should_be_reported();
    }

    // Test setup methods using TinyBDD
    private void Given.A_handler_with_mocked_dependencies()
    {
        _mockClient = Substitute.For<IPokManagerClient>();
        _handler = new RestartAllInstancesHandler(_mockClient);
    }

    // Additional setup and assertion methods...
}
```

#### Step 2: Create Request/Response DTOs

```csharp
// src/Core/PokManager.Application/UseCases/Instances/RestartAllInstances/RestartAllInstancesRequest.cs

public record RestartAllInstancesRequest
{
    public bool WaitForCompletion { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}
```

```csharp
// src/Core/PokManager.Application/UseCases/Instances/RestartAllInstances/RestartAllInstancesResponse.cs

public record RestartAllInstancesResponse
{
    public int TotalInstances { get; init; }
    public int SuccessfulRestarts { get; init; }
    public int FailedRestarts { get; init; }
    public IReadOnlyList<string> FailedInstanceIds { get; init; } = Array.Empty<string>();
}
```

#### Step 3: Implement Handler

```csharp
// src/Core/PokManager.Application/UseCases/Instances/RestartAllInstances/RestartAllInstancesHandler.cs

public class RestartAllInstancesHandler : IRestartAllInstancesHandler
{
    private readonly IPokManagerClient _client;
    private readonly ILogger<RestartAllInstancesHandler> _logger;

    public RestartAllInstancesHandler(
        IPokManagerClient client,
        ILogger<RestartAllInstancesHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<Result<RestartAllInstancesResponse>> HandleAsync(
        RestartAllInstancesRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get all instances
        var instancesResult = await _client.ListInstancesAsync(cancellationToken);
        if (!instancesResult.IsSuccess)
            return Result.Failure<RestartAllInstancesResponse>(instancesResult.Error);

        var instances = instancesResult.Value;
        var failedInstances = new List<string>();

        // Restart each instance
        foreach (var instanceId in instances)
        {
            var result = await _client.RestartInstanceAsync(instanceId, cancellationToken);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed to restart instance {InstanceId}: {Error}",
                    instanceId, result.Error);
                failedInstances.Add(instanceId);
            }
        }

        // Build response
        var response = new RestartAllInstancesResponse
        {
            TotalInstances = instances.Count,
            SuccessfulRestarts = instances.Count - failedInstances.Count,
            FailedRestarts = failedInstances.Count,
            FailedInstanceIds = failedInstances
        };

        return Result.Success(response);
    }
}
```

#### Step 4: Run Tests and Refactor

```bash
# Run tests
dotnet test tests/PokManager.Application.Tests/

# All tests should pass
# Refactor as needed while keeping tests green
```

## TDD with TinyBDD

### TinyBDD Basics

TinyBDD provides a behavior-driven testing approach with fluent syntax.

### Test Structure

```csharp
public class When_performing_action : Feature  // Feature base class from TinyBDD
{
    // Arrange - Test setup
    private readonly MyClass _sut;
    private readonly IMockDependency _mockDep;

    public When_performing_action()
    {
        // Constructor = "Given" setup
        _mockDep = Substitute.For<IMockDependency>();
        _sut = new MyClass(_mockDep);
    }

    // Act + Assert
    [Fact]
    public void Should_produce_expected_result()
    {
        // Arrange (additional)
        var input = "test";

        // Act
        var result = _sut.DoSomething(input);

        // Assert
        result.Should().Be("expected");
    }
}
```

### Given-When-Then Pattern

```csharp
public class When_starting_an_instance : Feature
{
    [Fact]
    public void Should_succeed_when_instance_is_stopped()
    {
        // Given
        Given.An_instance_exists();
        And.Instance_is_stopped();

        // When
        When.Starting_the_instance();

        // Then
        Then.Operation_should_succeed();
        And.Instance_should_be_running();
    }

    // Helper methods
    private void Given.An_instance_exists() { /* setup */ }
    private void And.Instance_is_stopped() { /* setup */ }
    private void When.Starting_the_instance() { /* action */ }
    private void Then.Operation_should_succeed() { /* assertion */ }
    private void And.Instance_should_be_running() { /* assertion */ }
}
```

### Test Naming Convention

Test class names follow the pattern: `When_[action]_[context]`

Test method names follow: `Should_[expected_result]_[condition]`

Examples:
- `When_starting_an_instance` / `Should_succeed_when_instance_is_stopped`
- `When_parsing_status_output` / `Should_parse_running_instance_correctly`
- `When_creating_a_backup` / `Should_fail_when_instance_not_found`

### FluentAssertions

Use FluentAssertions for readable assertions:

```csharp
// ✅ Good - Fluent and readable
result.IsSuccess.Should().BeTrue();
result.Value.Should().NotBeNull();
result.Value.InstanceId.Should().Be("server1");
failedInstances.Should().BeEmpty();

// ❌ Bad - Less readable
Assert.True(result.IsSuccess);
Assert.NotNull(result.Value);
Assert.Equal("server1", result.Value.InstanceId);
Assert.Empty(failedInstances);
```

### Mocking with NSubstitute

```csharp
// Create mock
var mockClient = Substitute.For<IPokManagerClient>();

// Setup return value
mockClient.StartInstanceAsync("server1")
    .Returns(Result.Success(Unit.Default));

// Verify call was made
await mockClient.Received(1).StartInstanceAsync("server1");

// Verify call was NOT made
await mockClient.DidNotReceive().StopInstanceAsync(Arg.Any<string>());
```

## Coding Standards

### C# Conventions

#### Naming

```csharp
// Classes, interfaces, methods - PascalCase
public class InstanceManager { }
public interface IPokManagerClient { }
public async Task StartInstanceAsync() { }

// Private fields - _camelCase with underscore
private readonly IPokManagerClient _client;
private string _instanceId;

// Local variables, parameters - camelCase
public void Process(string instanceId)
{
    var result = DoSomething();
}

// Constants - PascalCase
public const int MaxRetryAttempts = 3;

// Properties - PascalCase
public string InstanceId { get; set; }
```

#### File Organization

```csharp
// 1. Usings
using System;
using PokManager.Domain;

// 2. Namespace
namespace PokManager.Application.UseCases;

// 3. Class/Interface
public class StartInstanceHandler
{
    // 4. Constants
    private const int MaxRetries = 3;

    // 5. Fields
    private readonly IPokManagerClient _client;

    // 6. Constructor
    public StartInstanceHandler(IPokManagerClient client)
    {
        _client = client;
    }

    // 7. Public methods
    public async Task<Result<Unit>> HandleAsync() { }

    // 8. Private methods
    private void ValidateRequest() { }
}
```

### Code Style

#### Use Expression-Bodied Members

```csharp
// ✅ Good - Simple property
public bool IsRunning => State == InstanceState.Running;

// ✅ Good - Simple method
public string GetDisplayName() => $"{Name} ({Id})";

// ❌ Bad - Too complex for expression body
public bool IsValid() => !string.IsNullOrEmpty(Id) && State != InstanceState.Unknown && CreatedAt < DateTime.UtcNow;
```

#### Use Pattern Matching

```csharp
// ✅ Good - Pattern matching
return status switch
{
    InstanceState.Running => "The instance is running",
    InstanceState.Stopped => "The instance is stopped",
    InstanceState.Failed => "The instance has failed",
    _ => "Unknown status"
};

// ❌ Bad - If-else chain
if (status == InstanceState.Running)
    return "The instance is running";
else if (status == InstanceState.Stopped)
    return "The instance is stopped";
// ...
```

#### Prefer Records for DTOs

```csharp
// ✅ Good - Immutable record
public record StartInstanceRequest(string InstanceId, bool WaitForStart = true);

// ❌ Bad - Mutable class for DTO
public class StartInstanceRequest
{
    public string InstanceId { get; set; }
    public bool WaitForStart { get; set; }
}
```

#### Use Nullable Reference Types

```csharp
// Enabled in .csproj
<Nullable>enable</Nullable>

// ✅ Good - Explicit nullability
public string? ErrorMessage { get; set; }  // Can be null
public string InstanceId { get; set; }     // Never null

// Use null-forgiving operator sparingly
var value = nullableValue!;  // Only when you're certain
```

### Result<T> Pattern

Always use `Result<T>` for operations that can fail:

```csharp
// ✅ Good - Result<T>
public Result<InstanceStatus> GetStatus(string instanceId)
{
    if (string.IsNullOrEmpty(instanceId))
        return Result.Failure<InstanceStatus>("Instance ID is required");

    var status = FetchStatus(instanceId);
    return status != null
        ? Result.Success(status)
        : Result.Failure<InstanceStatus>("Instance not found");
}

// ❌ Bad - Throwing exceptions for flow control
public InstanceStatus GetStatus(string instanceId)
{
    if (string.IsNullOrEmpty(instanceId))
        throw new ArgumentException("Instance ID is required");

    var status = FetchStatus(instanceId);
    if (status == null)
        throw new InstanceNotFoundException();

    return status;
}
```

### XML Documentation

Add XML documentation for public APIs:

```csharp
/// <summary>
/// Starts the specified ARK server instance.
/// </summary>
/// <param name="instanceId">The unique identifier of the instance to start.</param>
/// <param name="cancellationToken">Cancellation token for the operation.</param>
/// <returns>A result indicating success or failure of the start operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when instanceId is null.</exception>
public async Task<Result<Unit>> StartInstanceAsync(
    string instanceId,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

## Testing Guidelines

### Test Coverage Requirements

- **Domain Layer**: 100% coverage (business logic must be fully tested)
- **Application Layer**: 95%+ coverage (all use cases and validation)
- **Infrastructure Layer**: 90%+ coverage (complex logic fully tested)
- **Presentation Layer**: 80%+ coverage (key components and workflows)

### Test Organization

```
tests/
├── PokManager.Domain.Tests/
│   ├── Common/
│   │   └── ResultTests.cs              # Result<T> tests
│   ├── Entities/
│   │   └── InstanceTests.cs            # Instance entity tests
│   └── ValueObjects/
│       └── InstanceIdTests.cs          # Value object tests
│
├── PokManager.Application.Tests/
│   ├── UseCases/
│   │   ├── Instances/
│   │   │   ├── StartInstanceHandlerTests.cs
│   │   │   └── StopInstanceHandlerTests.cs
│   │   └── Backups/
│   │       └── CreateBackupHandlerTests.cs
│   └── Validation/
│       └── StartInstanceValidatorTests.cs
│
├── PokManager.Infrastructure.Tests/
│   ├── PokManager/
│   │   ├── Parsers/
│   │   │   ├── StatusOutputParserTests.cs
│   │   │   └── BackupListParserTests.cs
│   │   └── PokManagerClientTests.cs
│   └── Docker/
│       └── DockerCommandBuilderTests.cs
│
└── PokManager.Web.Tests/
    └── Components/
        └── InstanceCardTests.cs
```

### Test Categories

Use categories to organize test runs:

```csharp
[Trait("Category", "Unit")]
public class When_parsing_status_output : Feature { }

[Trait("Category", "Integration")]
public class When_executing_real_commands : Feature { }

[Trait("Category", "E2E")]
public class When_user_starts_instance_via_ui : Feature { }
```

Run specific categories:

```bash
# Unit tests only
dotnet test --filter "Category=Unit"

# Integration tests
dotnet test --filter "Category=Integration"
```

### Test Data Builders

Use builders for complex test data:

```csharp
public class InstanceStatusBuilder
{
    private string _instanceId = "default-server";
    private InstanceState _state = InstanceState.Running;
    private ProcessHealth _health = ProcessHealth.Healthy;

    public InstanceStatusBuilder WithInstanceId(string id)
    {
        _instanceId = id;
        return this;
    }

    public InstanceStatusBuilder WithState(InstanceState state)
    {
        _state = state;
        return this;
    }

    public InstanceStatus Build()
    {
        return new InstanceStatus
        {
            InstanceId = _instanceId,
            State = _state,
            Health = _health
        };
    }
}

// Usage in tests
var status = new InstanceStatusBuilder()
    .WithInstanceId("test-server")
    .WithState(InstanceState.Stopped)
    .Build();
```

## Build System

### Building the Solution

```bash
# Full build
dotnet build

# Build specific configuration
dotnet build --configuration Release

# Build specific project
dotnet build src/Core/PokManager.Domain/PokManager.Domain.csproj

# Parallel build (faster)
dotnet build -m

# Verbose output
dotnet build --verbosity detailed
```

### Cleaning Build Artifacts

```bash
# Clean all projects
dotnet clean

# Clean and rebuild
dotnet clean && dotnet build
```

### Nuke Build Automation (Planned)

PokManagerApi will use Nuke for build automation:

```bash
# Install Nuke global tool
dotnet tool install Nuke.GlobalTool --global

# Run default build
nuke

# Run specific target
nuke Test

# Run clean build
nuke Clean Build Test
```

## IDE Setup

### Visual Studio 2022

#### Required Workloads
- ASP.NET and web development
- .NET desktop development
- .NET Aspire SDK

#### Recommended Extensions
- ReSharper or CodeMaid (code cleanup)
- Visual Studio Spell Checker
- Markdown Editor
- GitLens (if not using built-in Git)

#### Settings
1. **Tools > Options > Text Editor > C# > Code Style > General**
   - Set naming conventions
   - Enable EditorConfig support

2. **Tools > Options > Text Editor > C# > Advanced**
   - Enable "Place 'System' directives first when sorting usings"

3. **Test Explorer**
   - Group by: Project, Namespace, Class

### JetBrains Rider

#### Recommended Plugins
- TinyBDD Test Runner (if available)
- .NET Aspire Support
- Heap Allocations Viewer

#### Settings
1. **Settings > Editor > Code Style > C#**
   - Import code style from `.editorconfig`

2. **Settings > Build, Execution, Deployment > Unit Testing**
   - Enable continuous testing (optional)

### Visual Studio Code

#### Required Extensions
- C# Dev Kit (Microsoft)
- C# (Microsoft)
- .NET Aspire

#### Recommended Extensions
- GitLens
- Thunder Client (API testing)
- Markdown All in One
- Better Comments

#### Settings (`.vscode/settings.json`)
```json
{
  "dotnet.defaultSolution": "PokManager.sln",
  "omnisharp.enableRoslynAnalyzers": true,
  "omnisharp.enableEditorConfigSupport": true,
  "csharp.format.enable": true
}
```

## Debugging Tips

### Debugging Tests

#### Visual Studio
1. Set breakpoint in test
2. Right-click test in Test Explorer
3. Select "Debug"

#### Rider
1. Set breakpoint in test
2. Click debug icon next to test
3. Or use Ctrl+Shift+D

#### VS Code
1. Set breakpoint in test
2. Open Test Explorer
3. Right-click test > Debug Test

### Debugging Aspire Applications

```bash
# Run with debugger attached
dotnet run --project src/Hosting/PokManager.AppHost --launch-profile https

# View logs in Aspire Dashboard
# Navigate to https://localhost:15000
# Click on service to view logs
```

### Debugging Infrastructure Code

Add detailed logging:

```csharp
_logger.LogDebug("Executing command: {Command}", command);
_logger.LogDebug("Command output: {Output}", output);
```

View logs in:
- Console output
- Aspire Dashboard
- Log files (if configured)

### Common Debugging Scenarios

#### Issue: Test is failing unexpectedly

```csharp
// Add detailed assertion messages
result.IsSuccess.Should().BeTrue(
    "because the instance should start successfully when it's stopped");

// Log intermediate values
_testOutputHelper.WriteLine($"Result: {result}");
_testOutputHelper.WriteLine($"Error: {result.Error}");
```

#### Issue: Command execution is failing

```csharp
// Log command before execution
_logger.LogInformation("Executing: {Command}", command);

// Log raw output
_logger.LogDebug("Raw output: {Output}", rawOutput);
_logger.LogDebug("Raw error: {Error}", rawError);
```

## Git Workflow

### Branch Naming

- `feature/short-description` - New features
- `bugfix/issue-description` - Bug fixes
- `refactor/what-changed` - Refactoring
- `docs/what-documented` - Documentation
- `test/what-tested` - Test improvements

Examples:
- `feature/restart-all-instances`
- `bugfix/parser-null-reference`
- `refactor/command-builders`
- `docs/architecture-diagrams`

### Commit Messages

Follow Conventional Commits:

```
type(scope): subject

body (optional)

footer (optional)
```

**Types**:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation
- `test`: Tests
- `refactor`: Code refactoring
- `chore`: Maintenance

**Examples**:
```
feat(application): add restart all instances use case

Implements a new use case to restart all server instances
in parallel with failure handling.

Resolves #123

---

fix(parsers): handle null output in status parser

The status parser was throwing NullReferenceException
when given null input. Now returns Result.Failure.

---

test(domain): add Result<T> edge case tests

Adds tests for Result<T> chaining and error propagation.
```

### Pre-Commit Checklist

Before committing:

- ✅ All tests pass (`dotnet test`)
- ✅ Code builds without warnings (`dotnet build`)
- ✅ Code follows style guidelines
- ✅ New code has tests
- ✅ Documentation updated (if needed)
- ✅ No commented-out code
- ✅ No debug statements (Console.WriteLine, etc.)

## Code Review

### Submitting a PR

1. **Create a feature branch**
   ```bash
   git checkout -b feature/my-feature
   ```

2. **Write tests and implementation**
   - Follow TDD workflow
   - Ensure all tests pass

3. **Update documentation**
   - Update README if needed
   - Add/update XML docs
   - Update architecture docs if applicable

4. **Commit changes**
   ```bash
   git add .
   git commit -m "feat(scope): description"
   ```

5. **Push and create PR**
   ```bash
   git push origin feature/my-feature
   ```

### PR Description Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] All existing tests pass
- [ ] New tests added
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No new warnings

## Related Issues
Closes #123
```

### Code Review Checklist (Reviewer)

#### Architecture
- ✅ Follows Clean Architecture principles
- ✅ Correct layer placement
- ✅ Dependencies point inward

#### Testing
- ✅ Tests written before implementation (TDD)
- ✅ Tests are comprehensive
- ✅ Tests are readable and maintainable
- ✅ Edge cases covered

#### Code Quality
- ✅ Code is readable and maintainable
- ✅ Naming is clear and consistent
- ✅ No code duplication
- ✅ Proper error handling (Result<T>)
- ✅ XML documentation for public APIs

#### Performance
- ✅ No obvious performance issues
- ✅ Async/await used correctly
- ✅ No unnecessary allocations

---

**Happy coding! Remember: Tests first, then implementation!**
