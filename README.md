# PokManagerApi

> A comprehensive API and web interface for managing ARK: Survival Ascended servers via POK Manager

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](#)
[![Test Coverage](https://img.shields.io/badge/tests-904%20passing-brightgreen.svg)](#testing)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Aspire](https://img.shields.io/badge/Aspire-13.1.0-blue.svg)](https://learn.microsoft.com/dotnet/aspire/)
[![Code Quality](https://img.shields.io/badge/architecture-Clean%20Architecture-success.svg)](#architecture)

## Overview

PokManagerApi is a modern, production-ready control plane for managing ARK: Survival Ascended (ASA) server instances through POK Manager. Built with .NET 10, Aspire 13.1.0, and Blazor, it provides a comprehensive web UI and REST API for complete server lifecycle management.

The project emphasizes **Clean Architecture**, **Test-Driven Development (TDD)**, and **type-safe operations** with extensive test coverage (904 tests, 99.9% pass rate).

## Features

### Instance Management
- **Create Instance** - Deploy new ARK server instances from templates
- **Start/Stop/Restart** - Control instance lifecycle with safety checks
- **Status Monitoring** - Real-time server status and health checks
- **Log Management** - View, tail, and download server logs
- **Disk-Based Discovery** - Primary discovery via Instance_* directories (filesystem as source of truth)
- **Container Status Tracking** - Docker container state as secondary property
- **Data-Only Instances** - Support for instances without containers (e.g., TwinPeak)

### Container Lifecycle Management
- **Create Container** - Initialize containers from existing disk data
- **Destroy Container** - Remove containers while preserving data (safe by default)
- **Recreate Container** - Refresh container configuration without data loss
- **Docker Compose Integration** - Full docker-compose orchestration support

### Backup Management
- **Manual Backups** - Create on-demand backups with compression
- **Backup Listing** - View all available backups per instance
- **Restore Operations** - Revert to previous backups with preview
- **Backup Download** - Export backups for external storage
- **Retention Policies** - Configurable backup retention rules

### Configuration Management
- **View Configuration** - Display current instance settings
- **Edit Configuration** - Modify server settings with validation
- **Apply Changes** - Safe configuration updates with restart prompts
- **Template Support** - Use configuration templates for consistency
- **Validation** - FluentValidation ensures configuration correctness

### Updates
- **Update Detection** - Check for ASA and container updates
- **Safe Updates** - Gated update process with rollback support
- **Update History** - Track update history and changes
- **Scheduled Updates** - Configure automatic update windows

### Scheduling
- **Restart Schedules** - Cron-like restart scheduling
- **Backup Schedules** - Automated backup creation
- **Maintenance Windows** - Define blackout periods for operations
- **Task Management** - View and manage scheduled tasks

### Security & Safety
- **Operation Locking** - Prevent concurrent operations on instances
- **Audit Trails** - Complete audit log of all operations
- **Input Validation** - Comprehensive validation of all inputs
- **Plan-Preview-Apply** - Safe workflow for destructive operations
- **No Secret Leakage** - Redacted logging and secure handling

### Architecture
- **Clean Architecture** - Maintainable, testable, scalable design
- **TDD-First** - 904 comprehensive tests drive implementation
- **Type-Safe** - Strong typing throughout, no stringly-typed commands
- **Result<T> Monad** - Explicit error handling without exceptions

## Screenshots

> Placeholder for application screenshots

![Dashboard](docs/images/dashboard.png)
![Instance Management](docs/images/instance-management.png)
![Backup Management](docs/images/backup-management.png)

## Quick Start

### Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker** - For running ASA server containers
- **POK Manager** - Installed at `~/asa_server` (pokuser)
- **Linux Host** - Recommended for server operations
- **Git** - For cloning the repository

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/JerrettDavis/PokManagerUI.git
   cd PokManagerUI
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Run the application**
   ```bash
   dotnet run --project src/Hosting/PokManager.AppHost/PokManager.AppHost.csproj
   ```

6. **Access the application**
   - Web UI: `https://localhost:7000`
   - API Service: `https://localhost:7001`
   - Aspire Dashboard: `https://localhost:15000`

## Architecture

PokManagerApi follows **Clean Architecture** principles with clear separation of concerns:

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│  (Blazor Web UI, API Controllers)       │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│          Application Layer              │
│  (Use Cases, DTOs, Validation)          │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│        Infrastructure Layer             │
│  (POK Manager, Bash Execution)          │
└─────────────┬───────────────────────────┘
              │
┌─────────────▼───────────────────────────┐
│            Domain Layer                 │
│  (Entities, Value Objects, Business     │
│   Logic, No Dependencies)               │
└─────────────────────────────────────────┘
```

### Layer Responsibilities

- **Domain** - Pure business logic, entities, value objects, domain events (no dependencies)
- **Application** - Use cases, business rules, interfaces (depends on Domain)
- **Infrastructure** - External services, POK Manager client, file system (depends on Application)
- **Presentation** - Blazor UI, REST API, user interaction (depends on Application)

See [Architecture Documentation](docs/architecture.md) for detailed information.

## Technology Stack

### Core Frameworks
- **.NET 10** - Latest C# features and performance improvements
- **Aspire 13.1.0** - Cloud-native orchestration and observability
- **Blazor** - Interactive web UI with component-based architecture

### Testing
- **xUnit** - Primary testing framework
- **TinyBDD** - Behavior-driven development tests
- **FluentAssertions** - Readable test assertions
- **NSubstitute** - Mocking framework
- **bUnit** - Blazor component testing

### Infrastructure
- **FluentValidation** - Input validation library
- **YamlDotNet** - YAML configuration parsing
- **Microsoft.Extensions.*** - Logging, DI, Options pattern

### Build & Development
- **Nuke** - Build automation (planned)
- **Docker** - Containerization for deployment
- **Git** - Version control

## Project Structure

```
PokManagerApi/
├── src/
│   ├── Core/                            # Core business logic
│   │   ├── PokManager.Domain/           # Entities, value objects
│   │   └── PokManager.Application/      # Use cases, interfaces
│   │
│   ├── Infrastructure/                  # External integrations
│   │   ├── PokManager.Infrastructure/          # Base infrastructure
│   │   ├── PokManager.Infrastructure.PokManager/ # POK Manager client
│   │   └── PokManager.Infrastructure.Docker/    # Docker operations
│   │
│   ├── Presentation/                    # User interfaces
│   │   ├── PokManager.Web/              # Blazor web UI
│   │   └── PokManager.ApiService/       # REST API
│   │
│   └── Hosting/                         # Application hosting
│       ├── PokManager.AppHost/          # Aspire orchestration
│       └── PokManager.ServiceDefaults/  # Shared configurations
│
├── tests/                               # Test projects
│   ├── PokManager.Domain.Tests/         # 119 tests
│   ├── PokManager.Application.Tests/    # 390 tests
│   ├── PokManager.Infrastructure.Tests/ # 394 tests
│   └── PokManager.Web.Tests/            # 1 test
│
├── docs/                                # Documentation
│   ├── architecture.md                  # Architecture details
│   ├── getting-started.md               # Setup guide
│   ├── development.md                   # Development guide
│   └── README.md                        # Documentation index
│
└── samples/                             # Sample code and demos
```

## Configuration

### Application Settings

Configuration is managed through `appsettings.json` and environment variables:

```json
{
  "PokManager": {
    "BasePath": "~/asa_server",
    "User": "pokuser",
    "Timeout": "00:05:00",
    "AllowedOperations": ["start", "stop", "restart", "backup", "restore"]
  },
  "Backup": {
    "RetentionDays": 30,
    "CompressionFormat": "zst"
  },
  "Scheduling": {
    "Enabled": true,
    "DefaultRestartTime": "03:00:00"
  }
}
```

### Environment Variables

- `POKMANAGER_BASE_PATH` - POK Manager installation directory
- `POKMANAGER_USER` - Linux user for operations (default: pokuser)
- `ASPNETCORE_ENVIRONMENT` - Development/Staging/Production

### Configuration Validation

All configuration is validated at startup with clear error messages for invalid settings.

## Usage Examples

### Start an Instance (via API)

```bash
curl -X POST https://localhost:7001/api/instances/MyServer/start
```

### Create a Backup (via API)

```bash
curl -X POST https://localhost:7001/api/instances/MyServer/backups
```

### List Instances (via API)

```bash
curl https://localhost:7001/api/instances
```

### Using the Web UI

1. Navigate to `https://localhost:7000`
2. View the dashboard for all instances
3. Click on an instance to manage it
4. Use the action buttons to start/stop/restart
5. Navigate to Backups tab to manage backups

## Development

### Development Setup

See [Getting Started Guide](docs/getting-started.md) for detailed setup instructions.

### Building the Project

```bash
# Full build
dotnet build

# Build specific project
dotnet build src/Core/PokManager.Domain/PokManager.Domain.csproj

# Clean build
dotnet clean && dotnet build
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/PokManager.Domain.Tests/PokManager.Domain.Tests.csproj

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### TDD Workflow

This project uses **TinyBDD** for test-first development:

1. **Write TinyBDD tests** describing the behavior
2. **Add minimal scaffolding** to compile
3. **Implement** the simplest code to pass tests
4. **Refactor** while keeping tests green
5. **Add edge case coverage**

Example test structure:

```csharp
public class When_starting_an_instance : Feature
{
    [Fact]
    public void Should_return_success_when_instance_exists_and_stopped()
    {
        // Arrange
        Given.An_existing_stopped_instance();

        // Act
        When.Starting_the_instance();

        // Assert
        Then.Operation_should_succeed();
        And.Instance_should_be_running();
    }
}
```

### IDE Setup

- **Visual Studio 2022** - Full IDE support with Blazor tooling
- **JetBrains Rider** - Excellent C# and test runner support
- **VS Code** - Lightweight with C# Dev Kit extension

See [Development Guide](docs/development.md) for IDE configuration details.

## Testing

### Test Statistics

- **Total Tests**: 904
- **Passing**: 903 (99.9%)
- **Skipped**: 1 (0.1%)
- **Duration**: ~1-2 seconds

### Test Coverage by Layer

- **Domain Tests**: 119 tests - Core logic, Result<T>, value objects
- **Application Tests**: 390 tests - Use cases, validation, business rules
- **Infrastructure Tests**: 394 tests - POK Manager client, parsers, Docker
- **Web Tests**: 1 test - Blazor components

### Test Principles

- **TDD-First** - Tests written before implementation
- **Behavior-Driven** - Tests describe expected behavior
- **Fast** - All tests run in under 2 seconds
- **Isolated** - No dependencies on external services
- **Deterministic** - Consistent results every run

### Running Specific Tests

```bash
# Run domain tests only
dotnet test tests/PokManager.Domain.Tests

# Run tests matching a pattern
dotnet test --filter "FullyQualifiedName~StartInstance"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"
```

## Deployment

### Docker Deployment

```bash
# Build Docker image
docker build -t pokmanagerapi:latest .

# Run container
docker run -d -p 7000:8080 pokmanagerapi:latest
```

### Aspire Deployment

The project uses .NET Aspire for cloud-native deployment:

```bash
# Run with Aspire orchestration
dotnet run --project src/Hosting/PokManager.AppHost
```

### Production Considerations

- Configure reverse proxy (nginx, Caddy) for HTTPS
- Set up authentication/authorization
- Configure backup storage locations
- Set appropriate file permissions for pokuser
- Enable audit logging
- Configure monitoring and alerting

## Contributing

We welcome contributions! Please follow these guidelines:

### Getting Started

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Follow TDD workflow (write tests first!)
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Coding Standards

- Follow Clean Architecture principles
- Write TinyBDD tests before implementation
- Use Result<T> for error handling (no exceptions for flow control)
- Add XML documentation for public APIs
- Follow C# naming conventions
- Use meaningful variable names
- Keep methods focused and small

### Pull Request Process

1. Ensure all tests pass
2. Update documentation as needed
3. Add tests for new functionality
4. Follow the existing code style
5. Provide clear PR description

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- **POK Manager** - [Acekorneya/Ark-Survival-Ascended-Server](https://github.com/Acekorneya/Ark-Survival-Ascended-Server)
- **TinyBDD** - [jerrettdavis/tinybdd](https://github.com/jerrettdavis/tinybdd)
- **.NET Aspire** - [Microsoft .NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
- **Clean Architecture** - [Uncle Bob's Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

## Support

- **Documentation** - [docs/README.md](docs/README.md)
- **Issues** - [GitHub Issues](https://github.com/JerrettDavis/PokManagerUI/issues)
- **Discussions** - [GitHub Discussions](https://github.com/JerrettDavis/PokManagerUI/discussions)

## Roadmap

### Completed
- ✅ Clean Architecture foundation
- ✅ Domain layer with Result<T> monad
- ✅ Application layer use cases
- ✅ POK Manager integration
- ✅ Output parsers (5 parsers, 105 tests)
- ✅ 904 comprehensive tests
- ✅ Disk-based instance discovery (Phase 1)
- ✅ Container lifecycle management (Phase 2)
- ✅ Docker Compose integration

### In Progress
- 🔄 Blazor web UI components
- 🔄 REST API endpoints
- 🔄 Authentication/Authorization
- 🔄 Clone/Copy workflows (Phase 3)

### Planned
- 📋 Scheduling system
- 📋 Real-time monitoring dashboard
- 📋 Multi-server management
- 📋 Backup encryption
- 📋 Email notifications
- 📋 Performance metrics

---

**Built with ❤️ using .NET 10, Aspire 13.1.0, and Clean Architecture principles**
