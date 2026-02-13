---
name: cleanarchitecture
description: Ardalis Clean Architecture Solution Template for ASP.NET Core - dotnet template for creating DDD-based solutions with proper layer separation. Use when creating new Clean Architecture projects, organizing solution structure with Core/UseCases/Infrastructure/Web layers, implementing DDD patterns, setting up CQRS with MediatR, configuring FastEndpoints, or structuring enterprise .NET applications. Triggers on requests involving Clean Architecture template, layer dependencies, dependency inversion, domain-driven design solution structure, vertical slice architecture, or "dotnet new clean-arch" template usage.
---

# Clean Architecture Template Skill

Create well-structured ASP.NET Core solutions using Ardalis Clean Architecture Template - a proven starting point for DDD-based applications with proper layer separation and dependency inversion.

## What is Clean Architecture?

**Clean Architecture** (also known as Onion Architecture, Hexagonal Architecture, Ports & Adapters) is an architectural pattern that:
- **Inverts dependencies** so business logic doesn't depend on infrastructure
- **Separates concerns** into distinct layers
- **Protects domain** from external framework changes
- **Enables testing** by isolating business rules

**Key Principle**: Dependencies point **inward** toward the domain.

```
┌─────────────────────────────────────┐
│        Web (UI/API Layer)           │  ← Depends on UseCases & Infrastructure
├─────────────────────────────────────┤
│     Infrastructure (Details)        │  ← Depends on Core & UseCases
├─────────────────────────────────────┤
│   UseCases (Application Logic)      │  ← Depends on Core only
├─────────────────────────────────────┤
│      Core (Domain/Business)         │  ← No dependencies!
└─────────────────────────────────────┘
```

## Installation

### Install Template

```bash
# Install latest template
dotnet new install Ardalis.CleanArchitecture.Template

# Or specific version
dotnet new install Ardalis.CleanArchitecture.Template::11.0.0
```

### Create New Solution

```bash
# Navigate to parent directory
cd C:\Projects

# Create solution
dotnet new clean-arch -o YourCompany.ProjectName

# Result: YourCompany.ProjectName folder with full solution
```

**Important Notes:**
- Don't use hyphens in project name (use PascalCase)
- Don't use "Ardalis" as namespace (conflicts with dependencies)
- Template creates 4 projects + test projects

**Latest Version**: 11.0.0 (for .NET 10)
**GitHub**: https://github.com/ardalis/CleanArchitecture
**NuGet**: https://www.nuget.org/packages/Ardalis.CleanArchitecture.Template

## Solution Structure

### Full Template (4 Projects)

```
YourCompany.ProjectName/
├── src/
│   ├── YourCompany.ProjectName.Core/          # Domain layer
│   │   ├── Aggregates/
│   │   │   ├── ProjectAggregate/
│   │   │   │   ├── Project.cs                 # Aggregate root
│   │   │   │   ├── ToDoItem.cs               # Child entity
│   │   │   │   ├── Events/
│   │   │   │   │   └── NewItemAddedEvent.cs
│   │   │   │   └── Specifications/
│   │   │   │       └── ProjectByIdWithItemsSpec.cs
│   │   ├── ContributorAggregate/
│   │   │   └── Contributor.cs
│   │   └── Services/                          # Domain services
│   │
│   ├── YourCompany.ProjectName.UseCases/      # Application layer
│   │   ├── Contributors/
│   │   │   ├── Create/
│   │   │   │   ├── CreateContributorCommand.cs
│   │   │   │   └── CreateContributorHandler.cs
│   │   │   ├── Get/
│   │   │   │   ├── GetContributorQuery.cs
│   │   │   │   └── GetContributorHandler.cs
│   │   │   ├── List/
│   │   │   └── Delete/
│   │   └── Projects/
│   │       ├── Create/
│   │       ├── Get/
│   │       └── AddItem/
│   │
│   ├── YourCompany.ProjectName.Infrastructure/ # Infrastructure layer
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── EfRepository.cs
│   │   │   └── Config/
│   │   │       └── ProjectConfiguration.cs    # EF Core config
│   │   ├── Email/
│   │   │   └── FakeEmailSender.cs
│   │   ├── AutofacInfrastructureModule.cs     # DI registration
│   │   └── InfrastructureServiceExtensions.cs
│   │
│   └── YourCompany.ProjectName.Web/           # API/UI layer
│       ├── Endpoints/                         # FastEndpoints
│       │   ├── Contributors/
│       │   │   ├── Create.cs
│       │   │   ├── Get.cs
│       │   │   ├── List.cs
│       │   │   └── Delete.cs
│       │   └── Projects/
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── YourCompany.ProjectName.UnitTests/
    ├── YourCompany.ProjectName.IntegrationTests/
    └── YourCompany.ProjectName.FunctionalTests/
```

## Core Layer (Domain)

**No dependencies** - pure business logic.

### Entities & Aggregates

```csharp
using Ardalis.GuardClauses;
using Ardalis.SharedKernel;

namespace YourCompany.ProjectName.Core.ProjectAggregate;

public class Project : EntityBase, IAggregateRoot
{
    public string Name { get; private set; }
    public PriorityStatus Priority { get; private set; }

    private readonly List<ToDoItem> _items = new();
    public IReadOnlyCollection<ToDoItem> Items => _items.AsReadOnly();

    private Project() { } // EF Core

    public Project(string name, PriorityStatus priority)
    {
        Name = Guard.Against.NullOrEmpty(name);
        Priority = Guard.Against.Null(priority);

        RegisterDomainEvent(new ProjectCreatedEvent(this));
    }

    public void AddItem(ToDoItem newItem)
    {
        Guard.Against.Null(newItem);
        _items.Add(newItem);

        RegisterDomainEvent(new NewItemAddedEvent(this, newItem));
    }

    public void UpdateName(string newName)
    {
        Name = Guard.Against.NullOrEmpty(newName);
    }
}

public class ToDoItem : EntityBase
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsDone { get; private set; }

    public void MarkComplete()
    {
        if (!IsDone)
        {
            IsDone = true;
            RegisterDomainEvent(new ToDoItemCompletedEvent(this));
        }
    }
}
```

### Domain Events

```csharp
using Ardalis.SharedKernel;

namespace YourCompany.ProjectName.Core.ProjectAggregate.Events;

public class ProjectCreatedEvent : DomainEventBase
{
    public Project Project { get; }

    public ProjectCreatedEvent(Project project)
    {
        Project = project;
    }
}

public class NewItemAddedEvent : DomainEventBase
{
    public Project Project { get; }
    public ToDoItem NewItem { get; }

    public NewItemAddedEvent(Project project, ToDoItem newItem)
    {
        Project = project;
        NewItem = newItem;
    }
}
```

### Specifications

```csharp
using Ardalis.Specification;

namespace YourCompany.ProjectName.Core.ProjectAggregate.Specifications;

public class ProjectByIdWithItemsSpec : Specification<Project>
{
    public ProjectByIdWithItemsSpec(int projectId)
    {
        Query
            .Where(project => project.Id == projectId)
            .Include(project => project.Items);
    }
}

public class IncompleteItemsSpec : Specification<ToDoItem>
{
    public IncompleteItemsSpec()
    {
        Query.Where(item => !item.IsDone);
    }
}
```

## UseCases Layer (Application)

**Depends on Core only** - orchestrates domain logic.

### Commands (Write Operations)

```csharp
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace YourCompany.ProjectName.UseCases.Contributors.Create;

// Command
public record CreateContributorCommand(string Name) : ICommand<Result<int>>;

// Handler
public class CreateContributorHandler : ICommandHandler<CreateContributorCommand, Result<int>>
{
    private readonly IRepository<Contributor> _repository;

    public CreateContributorHandler(IRepository<Contributor> repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(
        CreateContributorCommand request,
        CancellationToken ct)
    {
        var newContributor = new Contributor(request.Name);
        var createdItem = await _repository.AddAsync(newContributor, ct);

        return Result<int>.Success(createdItem.Id);
    }
}
```

### Queries (Read Operations)

```csharp
using Ardalis.Result;
using Ardalis.SharedKernel;

namespace YourCompany.ProjectName.UseCases.Contributors.Get;

// Query
public record GetContributorQuery(int ContributorId) : IQuery<Result<ContributorDTO>>;

// DTO
public record ContributorDTO(int Id, string Name);

// Handler
public class GetContributorHandler : IQueryHandler<GetContributorQuery, Result<ContributorDTO>>
{
    private readonly IReadRepository<Contributor> _repository;

    public GetContributorHandler(IReadRepository<Contributor> repository)
    {
        _repository = repository;
    }

    public async Task<Result<ContributorDTO>> Handle(
        GetContributorQuery request,
        CancellationToken ct)
    {
        var contributor = await _repository.GetByIdAsync(request.ContributorId, ct);

        if (contributor == null)
            return Result<ContributorDTO>.NotFound();

        return Result<ContributorDTO>.Success(
            new ContributorDTO(contributor.Id, contributor.Name));
    }
}
```

## Infrastructure Layer

**Depends on Core & UseCases** - implements interfaces.

### EF Core DbContext

```csharp
using Ardalis.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace YourCompany.ProjectName.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDomainEventDispatcher dispatcher) : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ToDoItem> ToDoItems => Set<ToDoItem>();
    public DbSet<Contributor> Contributors => Set<Contributor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events after saving
        var entitiesWithEvents = ChangeTracker.Entries<HasDomainEventsBase>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        var result = await base.SaveChangesAsync(ct);

        await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);

        return result;
    }
}
```

### Repository Implementation

```csharp
using Ardalis.SharedKernel;
using Ardalis.Specification.EntityFrameworkCore;

namespace YourCompany.ProjectName.Infrastructure.Data;

public class EfRepository<T> : RepositoryBase<T>, IRepository<T>
    where T : class, IAggregateRoot
{
    public EfRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}
```

### Entity Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace YourCompany.ProjectName.Infrastructure.Data.Config;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(p => p.Name)
            .HasMaxLength(DataSchemaConstants.DEFAULT_NAME_LENGTH)
            .IsRequired();

        builder.Property(p => p.Priority)
            .HasConversion<int>();

        builder.HasMany(p => p.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(p => p.DomainEvents);
    }
}
```

### Service Registration

```csharp
using Ardalis.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace YourCompany.ProjectName.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("SqliteConnection");
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

        // Domain Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

        // Other services
        services.AddScoped<IEmailSender, FakeEmailSender>();

        return services;
    }
}
```

## Web Layer (API)

**Depends on UseCases & Infrastructure** - entry point.

### FastEndpoints

```csharp
using Ardalis.Result;
using FastEndpoints;
using MediatR;

namespace YourCompany.ProjectName.Web.Contributors;

// Create Endpoint
public class Create : Endpoint<CreateContributorRequest, CreateContributorResponse>
{
    private readonly IMediator _mediator;

    public Create(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override void Configure()
    {
        Post("/contributors");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        CreateContributorRequest request,
        CancellationToken ct)
    {
        var command = new CreateContributorCommand(request.Name);
        var result = await _mediator.Send(command, ct);

        if (result.IsSuccess)
        {
            await SendCreatedAtAsync<Get>(
                new { id = result.Value },
                new CreateContributorResponse(result.Value, request.Name),
                cancellation: ct);
        }
        else
        {
            await SendResultAsync(result.ToMinimalApiResult());
        }
    }
}

public record CreateContributorRequest(string Name);
public record CreateContributorResponse(int Id, string Name);

// Get Endpoint
public class Get : Endpoint<GetContributorRequest, ContributorRecord>
{
    private readonly IMediator _mediator;

    public Get(IMediator mediator) => _mediator = mediator;

    public override void Configure()
    {
        Get("/contributors/{ContributorId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(
        GetContributorRequest request,
        CancellationToken ct)
    {
        var query = new GetContributorQuery(request.ContributorId);
        var result = await _mediator.Send(query, ct);

        if (result.Status == ResultStatus.NotFound)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(
            new ContributorRecord(result.Value.Id, result.Value.Name),
            cancellation: ct);
    }
}

public record GetContributorRequest(int ContributorId);
public record ContributorRecord(int Id, string Name);
```

### Program.cs

```csharp
using Ardalis.SharedKernel;
using FastEndpoints;
using YourCompany.ProjectName.Infrastructure;
using YourCompany.ProjectName.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddFastEndpoints();
builder.Services.AddInfrastructureServices(builder.Configuration);

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(Contributor).Assembly);
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(context);
}

app.UseFastEndpoints();
app.Run();
```

## Testing

### Unit Tests (Core)

```csharp
using Xunit;

namespace YourCompany.ProjectName.UnitTests.Core.ProjectAggregate;

public class ProjectConstructor
{
    [Fact]
    public void InitializesName()
    {
        var project = new Project("Test Project", PriorityStatus.Backlog);

        Assert.Equal("Test Project", project.Name);
    }

    [Fact]
    public void RaisesProjectCreatedEvent()
    {
        var project = new Project("Test", PriorityStatus.Backlog);

        Assert.Single(project.DomainEvents);
        Assert.IsType<ProjectCreatedEvent>(project.DomainEvents.First());
    }
}
```

### Integration Tests (Infrastructure)

```csharp
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace YourCompany.ProjectName.IntegrationTests.Data;

public class EfRepositoryAdd : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly EfRepository<Contributor> _repository;

    public EfRepositoryAdd()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options, new FakeDomainEventDispatcher());
        _repository = new EfRepository<Contributor>(_dbContext);
    }

    [Fact]
    public async Task AddsContributorAndSetsId()
    {
        var contributor = new Contributor("Test");

        await _repository.AddAsync(contributor);

        Assert.True(contributor.Id > 0);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
```

### Functional Tests (API)

```csharp
using Ardalis.HttpClientTestExtensions;
using Xunit;
using Xunit.Abstractions;

namespace YourCompany.ProjectName.FunctionalTests.Contributors;

public class ContributorGetById : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public ContributorGetById(
        WebApplicationFactory<Program> factory,
        ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task ReturnsContributorById()
    {
        var result = await _client.GetAndDeserializeAsync<ContributorRecord>(
            "/contributors/1",
            _output);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }
}
```

## Best Practices

### 1. Keep Core Pure

```csharp
// ✅ Good - Core has no dependencies
namespace YourCompany.ProjectName.Core;

// ❌ Bad - Core references infrastructure
using Microsoft.EntityFrameworkCore; // Wrong!
```

### 2. Use Aggregates Properly

```csharp
// ✅ Good - Access items through aggregate root
var project = await _repository.GetByIdAsync(id);
project.AddItem(newItem);

// ❌ Bad - Direct ToDoItem repository
var item = new ToDoItem();
await _toDoItemRepository.AddAsync(item); // Wrong!
```

### 3. Organize by Feature

```csharp
// ✅ Good - vertical slices
UseCases/
  Contributors/
    Create/
    Get/
    Update/
    Delete/

// ❌ Bad - horizontal layers
UseCases/
  Commands/
  Queries/
  DTOs/
```

### 4. Use MediatR for Decoupling

```csharp
// ✅ Good - endpoint depends on MediatR
public class Create : Endpoint<Request, Response>
{
    private readonly IMediator _mediator;
    
    public async Task HandleAsync(Request req, CT ct)
    {
        var result = await _mediator.Send(new Command(req.Name), ct);
    }
}

// ❌ Bad - endpoint directly uses repository
public class Create : Endpoint<Request, Response>
{
    private readonly IRepository<Contributor> _repo; // Wrong!
}
```

### 5. Validate in Multiple Layers

```csharp
// Endpoint validation (input)
public class CreateContributorRequest
{
    public string Name { get; set; }
}

public class CreateValidator : Validator<CreateContributorRequest>
{
    public CreateValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

// Domain validation (business rules)
public Contributor(string name)
{
    Name = Guard.Against.NullOrEmpty(name);
    Guard.Against.InvalidFormat(name, nameof(name), @"^[a-zA-Z\s]+$");
}
```

## Version History

- **v11.0**: .NET 10, updated dependencies
- **v10.0**: .NET 9 support
- **v9.0**: FastEndpoints only (removed MVC/Razor Pages)
- **v8.0**: Added UseCases project, removed SharedKernel project
- **v7.1**: Last version with ApiEndpoints/Controllers/Razor Pages

## References

- GitHub: https://github.com/ardalis/CleanArchitecture
- NuGet Template: https://www.nuget.org/packages/Ardalis.CleanArchitecture.Template
- Course: https://www.pluralsight.com/courses/clean-architecture-introduction
- eBook: https://aka.ms/webappebook
- eShopOnWeb: https://github.com/dotnet-architecture/eShopOnWeb
- License: MIT
