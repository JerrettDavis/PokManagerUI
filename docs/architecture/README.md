# PokManagerApi - Clean Architecture Structure

## Overview

This solution follows **Clean Architecture** principles with a clear separation of concerns across layers.

## Solution Structure

```
PokManagerApi/
├── src/
│   ├── Core/                                    # Core Business Logic (no external dependencies)
│   │   ├── PokManager.Domain/                   # Domain entities, value objects, enums
│   │   │   └── Common/                          # Shared domain primitives (Result<T>, etc.)
│   │   └── PokManager.Application/              # Use cases, interfaces, business rules
│   │
│   ├── Infrastructure/                          # External concerns implementation
│   │   ├── PokManager.Infrastructure/           # Base infrastructure services
│   │   ├── PokManager.Infrastructure.PokManager/# PokManager-specific implementations
│   │   └── PokManager.Infrastructure.Docker/    # Docker-related implementations
│   │
│   ├── Presentation/                            # User interfaces and APIs
│   │   ├── PokManager.Web/                      # Blazor web frontend
│   │   └── PokManager.ApiService/               # REST API service
│   │
│   └── Hosting/                                 # Application hosting
│       ├── PokManager.AppHost/                  # .NET Aspire orchestration
│       └── PokManager.ServiceDefaults/          # Shared service configurations
│
├── tests/                                       # Test projects (TinyBDD)
│   ├── PokManager.Domain.Tests/
│   ├── PokManager.Application.Tests/
│   ├── PokManager.Infrastructure.Tests/
│   └── PokManager.Web.Tests/
│
├── docs/                                        # Documentation
│   └── architecture/
│
└── samples/                                     # Sample code and demos
```

## Dependency Rules

```
┌─────────────────────────────────────────────────┐
│              Presentation Layer                  │
│  (Web, ApiService) - User Interface/API         │
└────────────────┬────────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────────┐
│           Infrastructure Layer                   │
│  (Infrastructure.*) - External Services         │
└────────────────┬────────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────────┐
│            Application Layer                     │
│  Use Cases, Business Logic, Interfaces          │
└────────────────┬────────────────────────────────┘
                 │ depends on
┌────────────────▼────────────────────────────────┐
│              Domain Layer                        │
│  Entities, Value Objects (NO DEPENDENCIES)      │
└──────────────────────────────────────────────────┘
```

## Project Details

### Core Layer

#### PokManager.Domain (.NET 9)
- **Purpose**: Core domain entities and business rules
- **Dependencies**: None (pure domain logic)
- **Key Components**:
  - `Result<T>` monad for error handling
  - Domain entities
  - Value objects
  - Domain events

#### PokManager.Application (.NET 9)
- **Purpose**: Application use cases and business logic
- **Dependencies**: Domain
- **Packages**:
  - `FluentValidation` - Input validation
  - `Microsoft.Extensions.DependencyInjection.Abstractions` - DI

### Infrastructure Layer

#### PokManager.Infrastructure (.NET 9)
- **Purpose**: Base infrastructure services
- **Dependencies**: Application, Domain
- **Packages**:
  - `YamlDotNet` - YAML parsing
  - `Microsoft.Extensions.Logging`
  - `Microsoft.Extensions.Options`

#### PokManager.Infrastructure.PokManager (.NET 9)
- **Purpose**: PokManager-specific implementations
- **Dependencies**: Infrastructure, Application, Domain

#### PokManager.Infrastructure.Docker (.NET 9)
- **Purpose**: Docker-related implementations
- **Dependencies**: Infrastructure, Application, Domain

### Presentation Layer

#### PokManager.Web (.NET 10)
- **Purpose**: Blazor web frontend
- **Dependencies**: ServiceDefaults

#### PokManager.ApiService (.NET 10)
- **Purpose**: REST API service
- **Dependencies**: ServiceDefaults
- **Packages**: `Microsoft.AspNetCore.OpenApi`

### Hosting Layer

#### PokManager.AppHost (.NET 10)
- **Purpose**: .NET Aspire orchestration
- **SDK**: `Aspire.AppHost.Sdk/13.1.0`

#### PokManager.ServiceDefaults (.NET 10)
- **Purpose**: Shared service configurations

## Testing Strategy

All test projects use **TinyBDD** for behavior-driven development:

### PokManager.Domain.Tests (.NET 9)
- **Packages**: TinyBDD, xUnit, FluentAssertions
- **Coverage**: Domain logic, Result<T> monad (100% covered)

### PokManager.Application.Tests (.NET 9)
- **Packages**: TinyBDD, xUnit, FluentAssertions, NSubstitute
- **Focus**: Use case testing with mocked dependencies

### PokManager.Infrastructure.Tests (.NET 9)
- **Packages**: TinyBDD, xUnit, FluentAssertions, NSubstitute
- **Focus**: Infrastructure service testing

### PokManager.Web.Tests (.NET 10)
- **Packages**: TinyBDD, xUnit, FluentAssertions, bUnit
- **Focus**: Blazor component testing

## Key Design Patterns

### Result<T> Monad
Located in `PokManager.Domain.Common.Result<T>`, this pattern provides:
- Type-safe error handling
- No exceptions for flow control
- Explicit success/failure states
- `Unit` type for void operations

Example usage:
```csharp
public Result<User> GetUser(int id)
{
    if (id <= 0)
        return Result.Failure<User>("Invalid user ID");

    var user = FindUser(id);
    return user != null
        ? Result<User>.Success(user)
        : Result.Failure<User>("User not found");
}
```

## Build Status

- ✅ Solution builds without errors
- ✅ All Domain tests pass (6/6)
- ✅ Result<T> monad fully tested and covered
- ✅ Clean Architecture layers properly separated
- ✅ Dependency direction respected (inner layers have no knowledge of outer layers)

## Next Steps (Future Milestones)

- Implement domain entities
- Add use case implementations
- Create infrastructure services
- Build API endpoints
- Develop UI components
