# PokManagerApi Documentation

Welcome to the comprehensive documentation for PokManagerApi, a modern control plane for managing ARK: Survival Ascended servers via POK Manager.

## Documentation Index

### Getting Started
- **[Getting Started Guide](getting-started.md)** - Complete setup and installation instructions
  - Environment setup
  - Prerequisites installation
  - First-time configuration
  - Running the application
  - Troubleshooting common issues

### Architecture
- **[Architecture Overview](architecture.md)** - Detailed architecture documentation
  - Clean Architecture principles
  - Layer descriptions and responsibilities
  - Dependency rules and flow
  - Design patterns and practices
  - Testing strategy
  - Architecture diagrams

### Development
- **[Development Guide](development.md)** - Developer workflow and guidelines
  - Development workflow
  - TDD with TinyBDD
  - Coding standards and conventions
  - Testing guidelines
  - Build system (Nuke)
  - IDE setup and configuration
  - Debugging tips and tricks

### API Reference
- **[API Documentation](api-reference.md)** - REST API reference (Coming Soon)
  - Endpoint documentation
  - Request/response schemas
  - Authentication
  - Error handling

### Components
- **[Component Documentation](components.md)** - Blazor components (Coming Soon)
  - UI component library
  - Component usage examples
  - Styling guidelines

### Deployment
- **[Deployment Guide](deployment.md)** - Production deployment (Coming Soon)
  - Docker deployment
  - Aspire deployment
  - Cloud deployment options
  - Security considerations
  - Monitoring and logging

## Quick Links

### External Resources
- [POK Manager Repository](https://github.com/Acekorneya/Ark-Survival-Ascended-Server)
- [TinyBDD Framework](https://github.com/jerrettdavis/tinybdd)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Clean Architecture Principles](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

### Internal Documentation
- [Architecture Deep Dive](architecture/)
- [Command Builders Examples](command-builders-examples.md)
- [Agent Instructions](../AGENT.md)

## Documentation Standards

All documentation follows these principles:

1. **Clear and Concise** - Easy to understand with concrete examples
2. **Up-to-Date** - Documentation is updated with code changes
3. **Comprehensive** - Covers both happy paths and edge cases
4. **Practical** - Includes real-world usage examples
5. **Accessible** - Written for developers of all experience levels

## Contributing to Documentation

When contributing to the project, please also update relevant documentation:

- Update README.md for new features
- Add examples to getting-started.md for setup changes
- Document architectural changes in architecture.md
- Update development.md for new workflows or tools

## Documentation Structure

```
docs/
├── README.md                    # This file - documentation index
├── getting-started.md           # Setup and installation guide
├── architecture.md              # Architecture documentation
├── development.md               # Development workflow guide
├── api-reference.md             # REST API reference (planned)
├── components.md                # Blazor component docs (planned)
├── deployment.md                # Deployment guide (planned)
├── command-builders-examples.md # Command builder examples
├── architecture/                # Detailed architecture docs
│   └── README.md               # Architecture structure
└── images/                      # Screenshots and diagrams
    ├── dashboard.png
    ├── instance-management.png
    └── backup-management.png
```

## Finding What You Need

### For New Users
Start with the [Getting Started Guide](getting-started.md) to set up your development environment and run the application.

### For Developers
Review the [Development Guide](development.md) to understand the TDD workflow and coding standards.

### For Architects
Study the [Architecture Overview](architecture.md) to understand the system design and layer responsibilities.

### For DevOps
Check the [Deployment Guide](deployment.md) for production deployment strategies.

## Support

If you can't find what you're looking for in the documentation:

1. Check the [GitHub Issues](https://github.com/yourusername/PokManagerApi/issues)
2. Start a [Discussion](https://github.com/yourusername/PokManagerApi/discussions)
3. Review the [Agent Instructions](../AGENT.md) for detailed project guidelines

## Documentation Roadmap

### Completed
- ✅ Architecture documentation
- ✅ Getting started guide
- ✅ Development guide

### In Progress
- 🔄 API reference documentation
- 🔄 Component documentation
- 🔄 Deployment guide

### Planned
- 📋 Security best practices
- 📋 Performance tuning guide
- 📋 Backup and recovery procedures
- 📋 Monitoring and observability
- 📋 Migration guides
- 📋 FAQ and troubleshooting

---

**Last Updated**: 2026-01-19
**Version**: 1.0.0
**Maintainers**: PokManagerApi Team
