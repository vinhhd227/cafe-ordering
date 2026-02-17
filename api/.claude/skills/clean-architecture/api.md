# Clean Architecture API Reference

Template commands and project structure.

## Template Commands

```bash
# Install template
dotnet new install Ardalis.CleanArchitecture.Template

# Create solution
dotnet new clean-arch -o YourCompany.ProjectName

# Update template
dotnet new update
```

## Project Structure

```
Solution/
├── src/
│   ├── Core/           # Domain layer (no dependencies)
│   ├── UseCases/       # Application layer (depends on Core)
│   ├── Infrastructure/ # Infrastructure (depends on Core, UseCases)
│   └── Web/            # API layer (depends on all)
└── tests/
    ├── UnitTests/
    ├── IntegrationTests/
    └── FunctionalTests/
```

## Dependencies

**Core**: None (Ardalis.GuardClauses, Ardalis.SharedKernel only)
**UseCases**: Core only
**Infrastructure**: Core, UseCases
**Web**: Core, UseCases, Infrastructure

## Key Patterns

- **Repository Pattern**: IRepository<T>, IReadRepository<T>
- **Specification Pattern**: Ardalis.Specification
- **CQRS**: Commands/Queries with MediatR
- **Domain Events**: MediatR notifications
- **Result Pattern**: Ardalis.Result

See SKILL.md for implementation details.
