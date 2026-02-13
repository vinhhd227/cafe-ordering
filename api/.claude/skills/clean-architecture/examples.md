# Clean Architecture Examples

Complete implementation examples.

## Full Solution Example

```csharp
// Core/ProjectAggregate/Project.cs
public class Project : EntityBase, IAggregateRoot
{
    public string Name { get; private set; }
    private readonly List<ToDoItem> _items = new();
    public IReadOnlyCollection<ToDoItem> Items => _items.AsReadOnly();

    public Project(string name)
    {
        Name = Guard.Against.NullOrEmpty(name);
        RegisterDomainEvent(new ProjectCreatedEvent(this));
    }

    public void AddItem(ToDoItem item)
    {
        _items.Add(item);
        RegisterDomainEvent(new NewItemAddedEvent(this, item));
    }
}

// UseCases/Projects/Create/CreateProjectCommand.cs
public record CreateProjectCommand(string Name) : ICommand<Result<int>>;

public class CreateProjectHandler : ICommandHandler<CreateProjectCommand, Result<int>>
{
    private readonly IRepository<Project> _repository;

    public async Task<Result<int>> Handle(CreateProjectCommand command, CancellationToken ct)
    {
        var project = new Project(command.Name);
        await _repository.AddAsync(project, ct);
        return Result.Success(project.Id);
    }
}

// Infrastructure/Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public DbSet<Project> Projects => Set<Project>();

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var events = ChangeTracker.Entries<HasDomainEventsBase>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        var result = await base.SaveChangesAsync(ct);
        await _dispatcher.DispatchAndClearEvents(events);
        return result;
    }
}

// Web/Endpoints/Projects/Create.cs
public class Create : Endpoint<CreateProjectRequest, ProjectResponse>
{
    private readonly IMediator _mediator;

    public override void Configure()
    {
        Post("/projects");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateProjectRequest req, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProjectCommand(req.Name), ct);
        await SendCreatedAtAsync<Get>(new { id = result.Value }, 
            new ProjectResponse(result.Value, req.Name), cancellation: ct);
    }
}
```

See SKILL.md for complete documentation.
