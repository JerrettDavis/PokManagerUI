# Contributing to PokManagerApi

Thank you for your interest in contributing to PokManagerApi! This project provides a comprehensive web-based management interface for ARK: Survival Ascended servers using POK Manager. We welcome contributions from the community and appreciate your help in making this project better.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Contributing Code](#contributing-code)
  - [Improving Documentation](#improving-documentation)
  - [Writing Tests](#writing-tests)
- [Development Setup](#development-setup)
  - [Prerequisites](#prerequisites)
  - [Clone and Build](#clone-and-build)
  - [Running Tests](#running-tests)
  - [Running the Application](#running-the-application)
- [Code Style Guidelines](#code-style-guidelines)
  - [Architecture Principles](#architecture-principles)
  - [Naming Conventions](#naming-conventions)
  - [Error Handling](#error-handling)
- [Testing Guidelines](#testing-guidelines)
  - [TDD with TinyBDD](#tdd-with-tinybdd)
  - [Test Coverage Requirements](#test-coverage-requirements)
- [Commit Message Conventions](#commit-message-conventions)
- [Pull Request Process](#pull-request-process)
- [Code Review Process](#code-review-process)
- [Documentation Requirements](#documentation-requirements)
- [Community Guidelines](#community-guidelines)

## Code of Conduct

This project and everyone participating in it is governed by our [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating a bug report, please check the existing issues to avoid duplicates. When creating a bug report, include as many details as possible:

- Use a clear and descriptive title
- Describe the exact steps to reproduce the problem
- Provide specific examples to demonstrate the steps
- Describe the behavior you observed and what you expected to see
- Include screenshots if applicable
- Note your environment (.NET version, OS, browser if UI-related)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion:

- Use a clear and descriptive title
- Provide a detailed description of the proposed functionality
- Explain why this enhancement would be useful
- List any alternative solutions you've considered
- Include mockups or examples if applicable

### Contributing Code

We follow a test-driven development (TDD) approach using TinyBDD. All code contributions must include tests written before the implementation.

### Improving Documentation

Documentation improvements are always welcome! This includes:

- README updates
- API documentation
- Architecture documentation
- Code comments and XML documentation
- Tutorial and guide improvements

### Writing Tests

We use TinyBDD for behavior-driven testing. Contributing additional test coverage is highly valued, especially for:

- Edge cases
- Error conditions
- Integration scenarios
- Performance benchmarks

## Development Setup

### Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 10 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **.NET Aspire 13.1.0** - Will be installed with .NET 10 workloads
- **Git** - [Download here](https://git-scm.com/)
- **Docker** (optional, for local testing with POK Manager)
- **Visual Studio 2022** or **JetBrains Rider** (recommended) or **VS Code**

Install .NET Aspire workload:

```bash
dotnet workload install aspire
```

### Clone and Build

1. Fork the repository on GitHub
2. Clone your fork locally:

```bash
git clone https://github.com/YOUR-USERNAME/PokManagerApi.git
cd PokManagerApi
```

3. Add the upstream repository:

```bash
git remote add upstream https://github.com/ORIGINAL-OWNER/PokManagerApi.git
```

4. Build the solution:

```bash
dotnet build PokManager.sln
```

Or using the included build script (if Nuke is configured):

```bash
# Linux/macOS
./build.sh

# Windows
build.cmd
```

### Running Tests

Run all tests:

```bash
dotnet test
```

Run tests for a specific project:

```bash
dotnet test tests/PokManager.Domain.Tests
dotnet test tests/PokManager.Application.Tests
dotnet test tests/PokManager.Infrastructure.Tests
```

Run tests with coverage:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Running the Application

Using .NET Aspire AppHost:

```bash
dotnet run --project src/Hosting/PokManager.AppHost
```

The application will start and open the Aspire dashboard, typically at `http://localhost:15000`. From there you can access:

- **Web UI**: The Blazor web interface
- **API Service**: The backend API endpoints (if applicable)
- **Dashboard**: Aspire orchestration dashboard

## Code Style Guidelines

### Architecture Principles

This project follows **Clean Architecture** principles with clear separation of concerns:

- **Domain Layer** (`PokManager.Domain`)
  - Pure domain models (Instance, Backup, Schedule, etc.)
  - Domain invariants and value objects
  - No dependencies on other layers

- **Application Layer** (`PokManager.Application`)
  - Use cases and handlers (CreateInstance, StartInstance, RestoreBackup, etc.)
  - Interface definitions (ports) for infrastructure
  - Business logic orchestration

- **Infrastructure Layer** (`PokManager.Infrastructure.*`)
  - Concrete implementations of application interfaces
  - External service integrations (POK Manager, Docker, file system)
  - Database access, external APIs

- **Presentation Layer** (`PokManager.Web`, `PokManager.ApiService`)
  - Blazor components and pages
  - API controllers
  - Calls application layer only, never infrastructure directly

### Naming Conventions

- Use **PascalCase** for class names, method names, and public members
- Use **camelCase** for local variables and private fields
- Prefix interfaces with `I` (e.g., `IPokManagerClient`)
- Use descriptive names that express intent
- Avoid abbreviations unless widely understood

Example:

```csharp
public interface IPokManagerClient
{
    Task<Result<InstanceStatus>> GetInstanceStatusAsync(string instanceId);
}

public class PokManagerClient : IPokManagerClient
{
    private readonly ICommandExecutor _commandExecutor;

    public async Task<Result<InstanceStatus>> GetInstanceStatusAsync(string instanceId)
    {
        // Implementation
    }
}
```

### Error Handling

- Use `Result<T>` pattern for operations that can fail
- Never throw exceptions for expected error conditions
- Log exceptions with correlation IDs
- Never expose sensitive information in error messages
- Provide actionable error messages for users

Example:

```csharp
public async Task<Result<Instance>> CreateInstanceAsync(CreateInstanceRequest request)
{
    if (string.IsNullOrWhiteSpace(request.InstanceName))
        return Result<Instance>.Failure("Instance name is required");

    try
    {
        var instance = await _pokManagerClient.CreateAsync(request);
        await _auditSink.RecordAsync(new InstanceCreatedEvent(instance));
        return Result<Instance>.Success(instance);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to create instance {InstanceName}", request.InstanceName);
        return Result<Instance>.Failure("Failed to create instance");
    }
}
```

## Testing Guidelines

### TDD with TinyBDD

**Non-negotiable workflow**: Write tests BEFORE implementation code.

For each feature:

1. Write TinyBDD tests that describe the behavior
2. Add minimal scaffolding to make it compile
3. Implement the simplest code to make tests pass
4. Refactor while keeping tests green
5. Add coverage for edge cases and failure modes

Example TinyBDD test:

```csharp
public class CreateInstanceTests : ScenarioFor<CreateInstanceHandler>
{
    [Fact]
    public async Task Should_create_instance_when_valid_request()
    {
        var request = new CreateInstanceRequest("test-instance");

        await this.Given(s => s.APokManagerClient())
            .And(s => s.AValidConfiguration())
            .When(s => s.CreatingInstance(request))
            .Then(s => s.InstanceShouldBeCreated())
            .And(s => s.AuditEventShouldBeRecorded())
            .RunAsync();
    }

    [Fact]
    public async Task Should_fail_when_instance_name_is_empty()
    {
        var request = new CreateInstanceRequest("");

        await this.Given(s => s.APokManagerClient())
            .When(s => s.CreatingInstance(request))
            .Then(s => s.ResultShouldBeFailure())
            .And(s => s.ErrorShouldIndicate("Instance name is required"))
            .RunAsync();
    }
}
```

### Test Coverage Requirements

Every use case must have tests for:

- **Happy path**: Normal successful operation
- **Validation failures**: Invalid input, missing prerequisites
- **Command failures**: Simulated infrastructure failures
- **Concurrency**: Operations in progress, locking behavior
- **Audit**: Verification that audit events are emitted
- **Security**: Authorization, sensitive data handling

Aim for:
- Minimum 80% code coverage
- 100% coverage of critical paths (instance lifecycle, backups)

## Commit Message Conventions

We follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Types

- `feat`: A new feature
- `fix`: A bug fix
- `docs`: Documentation only changes
- `style`: Code style changes (formatting, missing semicolons, etc.)
- `refactor`: Code change that neither fixes a bug nor adds a feature
- `perf`: Performance improvements
- `test`: Adding or updating tests
- `chore`: Changes to build process, tooling, dependencies

### Examples

```
feat(backups): add automated backup scheduling

Implements Quartz-based scheduling for recurring backups.
Includes configuration options for cron expressions and retention policies.

Closes #123
```

```
fix(instance): prevent concurrent start/stop operations

Adds per-instance locking to prevent race conditions.

Fixes #456
```

```
test(domain): add edge cases for instance name validation
```

## Pull Request Process

1. **Create a feature branch** from `main`:
   ```bash
   git checkout -b feat/your-feature-name
   ```

2. **Write tests first** (TDD approach)
   - Create TinyBDD tests describing the desired behavior
   - Ensure tests fail initially (red phase)

3. **Implement your changes**
   - Write minimal code to make tests pass (green phase)
   - Refactor while keeping tests green
   - Follow code style guidelines

4. **Ensure all tests pass**:
   ```bash
   dotnet test
   ```

5. **Update documentation**
   - Update relevant README files
   - Add XML documentation to public APIs
   - Update architectural documentation if needed

6. **Commit your changes** using conventional commits:
   ```bash
   git add .
   git commit -m "feat(scope): description"
   ```

7. **Push to your fork**:
   ```bash
   git push origin feat/your-feature-name
   ```

8. **Create a Pull Request** on GitHub:
   - Use a clear, descriptive title
   - Describe what changes you made and why
   - Reference any related issues
   - Include screenshots for UI changes
   - Ensure CI checks pass

9. **Address review feedback**
   - Make requested changes
   - Push additional commits to your branch
   - Respond to comments

10. **Squash commits** (if requested by maintainers)
    - Keep commit history clean and meaningful

## Code Review Process

All submissions require review before merging. Reviewers will check:

- **Test coverage**: Are there TinyBDD tests? Do they cover edge cases?
- **Architecture**: Does it follow Clean Architecture principles?
- **Code quality**: Is the code readable, maintainable, and well-documented?
- **Security**: Are there any security concerns? Are secrets handled properly?
- **Performance**: Are there any obvious performance issues?
- **Documentation**: Is the code well-documented? Are README files updated?

Review timeline:
- Initial review within 3-5 business days
- Follow-up reviews within 1-2 business days

## Documentation Requirements

All contributions should include appropriate documentation:

### Code Documentation

- **XML documentation** for all public APIs:
  ```csharp
  /// <summary>
  /// Creates a new ASA instance with the specified configuration.
  /// </summary>
  /// <param name="request">The instance creation request containing name and settings.</param>
  /// <returns>A result containing the created instance or an error.</returns>
  public async Task<Result<Instance>> CreateInstanceAsync(CreateInstanceRequest request)
  ```

- **Inline comments** for complex logic (use sparingly)
- **README updates** for new features or changes to setup

### Architecture Documentation

For significant architectural changes, update documentation in the `docs/` folder:

- Architecture diagrams
- Design decisions
- Integration guides

### User Documentation

For user-facing features:

- Update user guides
- Add screenshots or videos
- Provide configuration examples

## Community Guidelines

### Be Respectful

- Treat everyone with respect and kindness
- Welcome newcomers and help them get started
- Provide constructive feedback
- Assume good intent

### Be Collaborative

- Share knowledge and learn from others
- Help review pull requests
- Participate in discussions
- Mentor new contributors

### Be Professional

- Keep discussions on-topic
- Avoid bikeshedding (excessive debate over minor details)
- Focus on technical merit
- Accept decisions gracefully

### Be Transparent

- Communicate your intentions clearly
- Share your progress and blockers
- Document your decisions
- Ask questions when uncertain

## Getting Help

If you need help:

- Check the [documentation](docs/)
- Review existing [issues](https://github.com/OWNER/PokManagerApi/issues)
- Ask questions in [discussions](https://github.com/OWNER/PokManagerApi/discussions)
- Join our community chat (if available)

See [SUPPORT.md](.github/SUPPORT.md) for more details.

## License

By contributing to PokManagerApi, you agree that your contributions will be licensed under the same license as the project.

Thank you for contributing to PokManagerApi!
