# SharedKernel API Reference

Complete API for Ardalis.SharedKernel base classes and interfaces.

## EntityBase

```csharp
public abstract class EntityBase
{
    public int Id { get; protected set; }
    
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(EntityBase? left, EntityBase? right);
    public static bool operator !=(EntityBase? left, EntityBase? right);
}

public abstract class EntityBase<TId>
{
    public TId Id { get; protected set; }
    // Same equality members as above
}
```

## HasDomainEventsBase

```csharp
public abstract class HasDomainEventsBase : EntityBase
{
    private List<DomainEventBase> _domainEvents = new();
    
    public IEnumerable<DomainEventBase> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void RegisterDomainEvent(DomainEventBase domainEvent);
    protected void ClearDomainEvents();
}
```

## ValueObject

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object? obj);
    public override int GetHashCode();
    public static bool operator ==(ValueObject? left, ValueObject? right);
    public static bool operator !=(ValueObject? left, ValueObject? right);
}
```

## Interfaces

```csharp
// Marker for aggregate roots
public interface IAggregateRoot { }

// Domain events
public abstract class DomainEventBase : INotification
{
    public DateTime DateOccurred { get; protected set; }
}

// Repositories
public interface IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default);
    Task<List<T>> ListAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}

public interface IRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// CQRS
public interface ICommand : IRequest<r> { }
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface IQuery<out TResponse> : IRequest<TResponse> { }
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result> { }
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> { }

// Domain Event Dispatcher
public interface IDomainEventDispatcher
{
    Task DispatchAndClearEvents(IEnumerable<HasDomainEventsBase> entitiesWithEvents);
}
```

See SKILL.md for usage examples.
