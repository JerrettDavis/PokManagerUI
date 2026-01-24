# Getting Started Guide

This guide will help you set up your development environment and get PokManagerApi running on your local machine.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Environment Setup](#environment-setup)
- [Installation](#installation)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Verifying the Setup](#verifying-the-setup)
- [Common Issues](#common-issues)
- [Next Steps](#next-steps)

## Prerequisites

Before you begin, ensure you have the following installed:

### Required Software

#### 1. .NET 10 SDK
- **Download**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Verify Installation**:
  ```bash
  dotnet --version
  # Should output: 10.0.x or higher
  ```

#### 2. Docker
- **Download**: [Docker Desktop](https://www.docker.com/products/docker-desktop)
- **Verify Installation**:
  ```bash
  docker --version
  # Should output: Docker version 24.x or higher
  ```

#### 3. Git
- **Download**: [Git](https://git-scm.com/downloads)
- **Verify Installation**:
  ```bash
  git --version
  # Should output: git version 2.x or higher
  ```

### Optional but Recommended

#### 1. IDE/Editor
Choose one:
- **Visual Studio 2022** (17.12+) - Full-featured IDE with Blazor support
  - [Download Professional/Community](https://visualstudio.microsoft.com/downloads/)
  - Workloads: "ASP.NET and web development", ".NET Aspire SDK"

- **JetBrains Rider** (2024.3+) - Excellent for C# development
  - [Download](https://www.jetbrains.com/rider/)

- **Visual Studio Code** - Lightweight with extensions
  - [Download](https://code.visualstudio.com/)
  - Extensions: C# Dev Kit, Blazor WASM Debugging

#### 2. POK Manager (For Production Use)
- **Repository**: [Ark-Survival-Ascended-Server](https://github.com/Acekorneya/Ark-Survival-Ascended-Server)
- **Installation**: Follow POK Manager installation guide
- **Default Location**: `~/asa_server` (on Linux)

### System Requirements

- **OS**: Windows 10/11, Linux (Ubuntu 20.04+), macOS 12+
- **RAM**: 8 GB minimum, 16 GB recommended
- **Disk Space**: 10 GB free space
- **CPU**: Multi-core processor recommended for development

## Environment Setup

### 1. Clone the Repository

```bash
# Clone the repository
git clone https://github.com/yourusername/PokManagerApi.git

# Navigate to the project directory
cd PokManagerApi
```

### 2. Restore .NET Tools

```bash
# Restore .NET local tools (if any)
dotnet tool restore
```

### 3. Verify Project Structure

```bash
# List directory structure
ls -la

# You should see:
# - src/           (source code)
# - tests/         (test projects)
# - docs/          (documentation)
# - PokManager.sln (solution file)
```

### 4. Restore NuGet Packages

```bash
# Restore all NuGet packages
dotnet restore

# This may take a few minutes on first run
```

### 5. Build the Solution

```bash
# Build entire solution
dotnet build

# Verify build succeeds with no errors
# Output should end with: Build succeeded.
```

## Installation

### Step 1: Install Dependencies

All dependencies are managed via NuGet and should be installed automatically during `dotnet restore`.

**Key Dependencies**:
- Aspire 13.1.0
- FluentValidation
- xUnit
- TinyBDD
- NSubstitute
- bUnit

### Step 2: Configure User Secrets (Optional)

For sensitive configuration (not committed to source control):

```bash
# Initialize user secrets for AppHost
cd src/Hosting/PokManager.AppHost
dotnet user-secrets init

# Set a secret (example)
dotnet user-secrets set "PokManager:BasePath" "/home/pokuser/asa_server"

# Return to root
cd ../../..
```

### Step 3: Configure appsettings.json (Development)

Create or modify `src/Presentation/PokManager.Web/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "PokManager": {
    "BasePath": "~/asa_server",
    "User": "pokuser",
    "Timeout": "00:05:00",
    "AllowedOperations": [
      "start",
      "stop",
      "restart",
      "backup",
      "restore"
    ]
  },
  "Backup": {
    "RetentionDays": 30,
    "CompressionFormat": "zst",
    "BackupDirectory": "~/asa_server/backups"
  },
  "Scheduling": {
    "Enabled": false,
    "DefaultRestartTime": "03:00:00"
  }
}
```

## Configuration

### Environment Variables

Set environment-specific configuration via environment variables:

#### Windows (PowerShell)
```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
$env:POKMANAGER_BASE_PATH="C:\POK\asa_server"
$env:POKMANAGER_USER="Administrator"
```

#### Linux/macOS (Bash)
```bash
export ASPNETCORE_ENVIRONMENT=Development
export POKMANAGER_BASE_PATH=/home/pokuser/asa_server
export POKMANAGER_USER=pokuser
```

### Configuration Hierarchy

Configuration is loaded in this order (later overrides earlier):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

### Key Configuration Sections

#### PokManager Settings

```json
{
  "PokManager": {
    "BasePath": "~/asa_server",           // POK Manager install path
    "User": "pokuser",                    // Linux user for operations
    "Timeout": "00:05:00",                // Command timeout
    "AllowedOperations": [                // Enabled operations
      "start", "stop", "restart",
      "backup", "restore"
    ]
  }
}
```

#### Backup Settings

```json
{
  "Backup": {
    "RetentionDays": 30,                  // Backup retention period
    "CompressionFormat": "zst",           // Compression: gz or zst
    "BackupDirectory": "~/asa_server/backups"
  }
}
```

#### Logging Settings

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PokManager": "Debug",              // Detailed POK Manager logs
      "Microsoft": "Warning"
    }
  }
}
```

## Running the Application

### Option 1: Run with .NET Aspire (Recommended)

This starts all services (Web UI + API + Aspire Dashboard):

```bash
# From project root
dotnet run --project src/Hosting/PokManager.AppHost/PokManager.AppHost.csproj
```

**Endpoints**:
- **Aspire Dashboard**: `https://localhost:15000` (or `http://localhost:15001`)
- **Web UI**: `https://localhost:7000`
- **API Service**: `https://localhost:7001`

The Aspire Dashboard shows:
- Service health
- Logs from all services
- Distributed traces
- Metrics

### Option 2: Run Web UI Only

```bash
# Run Blazor web UI
dotnet run --project src/Presentation/PokManager.Web/PokManager.Web.csproj
```

**Access**: `https://localhost:7000` (port may vary)

### Option 3: Run API Service Only

```bash
# Run API service
dotnet run --project src/Presentation/PokManager.ApiService/PokManager.ApiService.csproj
```

**Access**:
- API: `https://localhost:7001`
- Swagger UI: `https://localhost:7001/swagger`

### Option 4: Run with Watch (Hot Reload)

For development with automatic rebuilds on file changes:

```bash
# Watch mode
dotnet watch --project src/Presentation/PokManager.Web/PokManager.Web.csproj
```

## Verifying the Setup

### 1. Run Tests

Verify your setup by running all tests:

```bash
# Run all tests
dotnet test

# Expected output:
# Passed:   903
# Failed:   0
# Skipped:  1
# Total:    904
```

### 2. Access the Web UI

1. Navigate to `https://localhost:7000`
2. You should see the PokManagerApi dashboard
3. If POK Manager is installed, instances should be listed

### 3. Access the API

Test the API with curl:

```bash
# Health check
curl https://localhost:7001/health

# List instances (requires POK Manager)
curl https://localhost:7001/api/instances
```

### 4. Check Aspire Dashboard

1. Navigate to `https://localhost:15000`
2. Verify services are running:
   - ✅ PokManager.Web (green)
   - ✅ PokManager.ApiService (green)
3. Check logs for any errors

## Common Issues

### Issue 1: Port Already in Use

**Error**: `Failed to bind to address https://localhost:7000`

**Solution**:
```bash
# Find and kill process using port 7000
# Windows
netstat -ano | findstr :7000
taskkill /PID <PID> /F

# Linux/macOS
lsof -ti:7000 | xargs kill -9
```

### Issue 2: SDK Not Found

**Error**: `The specified SDK 'Aspire.AppHost.Sdk/13.1.0' was not found`

**Solution**:
```bash
# Install .NET Aspire workload
dotnet workload install aspire

# Verify installation
dotnet workload list
```

### Issue 3: Build Errors After Pull

**Error**: Various build errors after `git pull`

**Solution**:
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Issue 4: Certificate Trust Issues (HTTPS)

**Error**: `Unable to configure HTTPS endpoint`

**Solution**:
```bash
# Trust development certificate
dotnet dev-certs https --trust

# If that fails, clean and recreate
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Issue 5: POK Manager Not Found

**Error**: Operations fail with "POK Manager not found"

**Solution**:
1. Verify POK Manager installation: `ls ~/asa_server`
2. Check configuration in `appsettings.json`
3. Set correct `BasePath` in configuration
4. Verify user permissions

### Issue 6: Tests Failing

**Error**: Tests fail to run or produce errors

**Solution**:
```bash
# Rebuild test projects
dotnet clean
dotnet build tests/PokManager.Domain.Tests/PokManager.Domain.Tests.csproj
dotnet test tests/PokManager.Domain.Tests/PokManager.Domain.Tests.csproj

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```

### Issue 7: Docker Not Available

**Error**: Docker commands fail

**Solution**:
1. Ensure Docker Desktop is running
2. Verify Docker daemon: `docker ps`
3. Restart Docker Desktop if needed
4. Check Docker service: `systemctl status docker` (Linux)

## Next Steps

Now that you have PokManagerApi running:

### 1. Explore the Application
- Browse the Web UI at `https://localhost:7000`
- Review the Aspire Dashboard
- Test API endpoints via Swagger

### 2. Review Documentation
- **[Architecture Guide](architecture.md)** - Understand the system design
- **[Development Guide](development.md)** - Learn the development workflow
- **[API Reference](api-reference.md)** - Explore API endpoints

### 3. Start Developing
- Review the [TDD workflow](development.md#tdd-workflow)
- Explore existing tests in `tests/` directory
- Try implementing a new feature following TDD

### 4. Configure for Production
If deploying to production:
- Set up proper authentication
- Configure production database
- Set up reverse proxy (nginx, Caddy)
- Configure monitoring and logging
- Review [Deployment Guide](deployment.md)

## Getting Help

If you encounter issues not covered here:

1. **Check Documentation**: Review docs in `docs/` directory
2. **Search Issues**: Look for similar issues on GitHub
3. **Ask Questions**: Open a discussion on GitHub
4. **Report Bugs**: Create an issue with detailed information

## Development Environment Checklist

Before starting development, ensure:

- ✅ .NET 10 SDK installed and verified
- ✅ Docker installed and running
- ✅ Repository cloned
- ✅ Dependencies restored (`dotnet restore`)
- ✅ Solution builds successfully (`dotnet build`)
- ✅ All tests pass (`dotnet test`)
- ✅ Application runs (`dotnet run`)
- ✅ Web UI accessible at `https://localhost:7000`
- ✅ API accessible at `https://localhost:7001`
- ✅ IDE configured with recommended extensions

## Recommended First Tasks

To familiarize yourself with the codebase:

1. **Run Tests**: `dotnet test` and review test structure
2. **Explore Domain**: Review `src/Core/PokManager.Domain/`
3. **Review Use Cases**: Check `src/Core/PokManager.Application/`
4. **Study Parsers**: Look at `src/Infrastructure/PokManager.Infrastructure.PokManager/PokManager/Parsers/`
5. **Test the API**: Use Swagger UI at `https://localhost:7001/swagger`

## Additional Resources

- **POK Manager Docs**: https://github.com/Acekorneya/Ark-Survival-Ascended-Server
- **.NET Aspire**: https://learn.microsoft.com/dotnet/aspire/
- **TinyBDD**: https://github.com/jerrettdavis/tinybdd
- **Clean Architecture**: https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html

---

**Welcome to PokManagerApi development! Happy coding!**
