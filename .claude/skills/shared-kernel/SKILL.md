---
name: sharedkernel
description: Ardalis SharedKernel library for .NET - foundational DDD building blocks for Clean Architecture. Use when implementing Domain-Driven Design, creating domain entities and aggregates, value objects, domain events, repository interfaces, or CQRS patterns. Triggers on requests involving EntityBase, HasDomainEventsBase, ValueObject, IAggregateRoot, IRepository, IReadRepository, DomainEventBase, ICommand, IQuery, ICommandHandler, IQueryHandler, MediatR integration, or creating custom SharedKernel packages. Also use for questions about DDD base classes, entity identity, domain event publishing, repository patterns, or Clean Architecture core layer setup.
---

# SharedKernel Skill

Build Domain-Driven Design applications using Ardalis.SharedKernel - a collection of foundational base classes and interfaces for Clean Architecture and DDD patterns.

## What is SharedKernel?

In Domain-Driven Design (DDD), a **Shared Kernel** is a subset of the domain model that multiple bounded contexts agree to share. It typically contains common base classes, value objects, and interfaces that need to be consistent across the application.

**Ardalis.SharedKernel** provides:
- Base classes for Entities and Aggregates
- Value Object base class
- Domain Events infrastructure
- Repository interfaces (read/write separation)
- CQRS base classes (Commands, Queries, Handlers)
- MediatR integration for domain events

**IMPORTANT**: This library is intended as a **template/reference**, not for direct production use. You should fork/copy these classes and create your own `YourCompany.SharedKernel` package!

## Why Use SharedKernel?

**Problems it solves:**
- Repetitive base class code across entities
- Inconsistent entity identity and equality logic
- No standard domain event infrastructure
- Duplicate repository interface definitions
- CQRS pattern boilerplate
- Coupling between domain and infrastructure layers

**Benefits:**
- **DDD Building Blocks**: Standard base classes for entities, value objects, aggregates
- **Domain Events**: Built-in event publishing infrastructure
- **Repository Abstraction**: Clean separation between domain and data access
- **CQRS Support**: Base classes for commands, queries, and handlers
- **MediatR Integration**: Ready-to-use domain event dispatcher
- **Type Safety**: Generic constraints ensure proper usage
- **Testability**: Interfaces enable easy mocking

## Installation

```bash
# Main package
dotnet add package Ardalis.SharedKernel

# For creating your own SharedKernel template
dotnet new install Ardalis.SharedKernel.Template
```

**Latest Version**: 5.0.0+
**GitHub**: https://github.com/ardalis/Ardalis.SharedKernel
**NuGet**: https://www.nuget.org/packages/Ardalis.SharedKernel
**Recommended**: Create your own `YourCompany.SharedKernel` based on this

## Core Components

### 1. EntityBase

Base class for all entities with identity.

```csharp
using Ardalis.SharedKernel;

public class Customer : EntityBase
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }

    private Customer() { } // EF Core

    public Customer(string firstName, string lastName, string email)
    {
        FirstName = Guard.Against.NullOrEmpty(firstName);
        LastName = Guard.Against.NullOrEmpty(lastName);
        Email = Guard.Against.NullOrEmpty(email);
    }

    public void UpdateName(string firstName, string lastName)
    {
        FirstName = Guard.Against.NullOrEmpty(firstName);
        LastName = Guard.Against.NullOrEmpty(lastName);
    }
}

// Usage
var customer = new Customer("John", "Doe", "john@example.com");
Console.WriteLine(customer.Id); // Auto-generated ID
```

**EntityBase provides:**
- `int Id { get; protected set; }` - Primary key
- Equality comparison based on Id
- `GetHashCode()` override
- Proper equality operators (`==`, `!=`)

### 2. EntityBase with Typed ID

For entities with non-int IDs (Guid, long, etc.):

```csharp
public class Product : EntityBase<Guid>
{
    public string Name { get; private set; }
    public decimal Price { get; private set; }

    private Product() { }

    public Product(string name, decimal price)
    {
        Id = Guid.NewGuid(); // Set custom ID
        Name = Guard.Against.NullOrEmpty(name);
        Price = Guard.Against.Negative(price);
    }
}

// Usage
var product = new Product("Laptop", 999.99m);
Console.WriteLine(product.Id); // Guid ID
```

### 3. HasDomainEventsBase

Base class for entities that raise domain events.

```csharp
public class Order : HasDomainEventsBase, IAggregateRoot
{
    public int CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(int customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        
        // Raise domain event
        RegisterDomainEvent(new OrderCreatedEvent(this));
    }

    public void AddItem(Product product, int quantity)
    {
        var item = new OrderItem(product.Id, quantity, product.Price);
        _items.Add(item);
        RecalculateTotal();
        
        RegisterDomainEvent(new OrderItemAddedEvent(this, item));
    }

    public void Submit()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Order already submitted");

        Status = OrderStatus.Submitted;
        RegisterDomainEvent(new OrderSubmittedEvent(this));
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.Subtotal);
    }
}

// Domain Events
public class OrderCreatedEvent : DomainEventBase
{
    public Order Order { get; }
    public OrderCreatedEvent(Order order) => Order = order;
}

public class OrderSubmittedEvent : DomainEventBase
{
    public Order Order { get; }
    public OrderSubmittedEvent(Order order) => Order = order;
}
```

**HasDomainEventsBase provides:**
- `IEnumerable<DomainEventBase> DomainEvents { get; }` - Read-only event list
- `void RegisterDomainEvent(DomainEventBase domainEvent)` - Add event
- `void ClearDomainEvents()` - Clear all events

### 4. ValueObject

Base class for value objects (no identity, equality by value).

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money() { }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = Guard.Against.NullOrEmpty(currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");

        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

// Usage
var price1 = new Money(100.50m, "USD");
var price2 = new Money(100.50m, "USD");
var price3 = new Money(200.00m, "USD");

Console.WriteLine(price1 == price2); // True - same values
Console.WriteLine(price1 == price3); // False - different amounts

var total = price1.Add(price2); // 201.00 USD
```

**More Value Object Examples:**

```csharp
public class Address : ValueObject
{
    public string Street { get; private set; }
    public string City { get; private set; }
    public string State { get; private set; }
    public string ZipCode { get; private set; }

    private Address() { }

    public Address(string street, string city, string state, string zipCode)
    {
        Street = Guard.Against.NullOrEmpty(street);
        City = Guard.Against.NullOrEmpty(city);
        State = Guard.Against.NullOrEmpty(state);
        ZipCode = Guard.Against.NullOrEmpty(zipCode);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return State;
        yield return ZipCode;
    }
}

public class EmailAddress : ValueObject
{
    public string Value { get; private set; }

    private EmailAddress() { }

    public EmailAddress(string value)
    {
        Value = Guard.Against.NullOrEmpty(value);
        Guard.Against.InvalidFormat(value, nameof(value), @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public override string ToString() => Value;
}

public class DateRange : ValueObject
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }

    private DateRange() { }

    public DateRange(DateTime start, DateTime end)
    {
        Guard.Against.OutOfRange(start, nameof(start), start, end);
        Start = start;
        End = end;
    }

    public int DurationInDays() => (End - Start).Days;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
```

### 5. IAggregateRoot

Marker interface for aggregate roots.

```csharp
public class Order : HasDomainEventsBase, IAggregateRoot
{
    // Only aggregate roots should be accessed via repository
}

// Repository constraint
public interface IRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    // Only works with aggregate roots
}
```

**Why use IAggregateRoot?**
- **Enforces DDD rules**: Only aggregate roots accessed via repositories
- **Prevents data integrity issues**: Children accessed through aggregate root
- **Clear boundaries**: Defines transactional consistency boundaries

### 6. DomainEventBase

Base class for domain events.

```csharp
public abstract class DomainEventBase
{
    public DateTime DateOccurred { get; protected set; } = DateTime.UtcNow;
}

// Custom events
public class CustomerRegisteredEvent : DomainEventBase
{
    public int CustomerId { get; }
    public string Email { get; }

    public CustomerRegisteredEvent(int customerId, string email)
    {
        CustomerId = customerId;
        Email = email;
    }
}

public class OrderPlacedEvent : DomainEventBase
{
    public int OrderId { get; }
    public int CustomerId { get; }
    public decimal TotalAmount { get; }

    public OrderPlacedEvent(int orderId, int customerId, decimal totalAmount)
    {
        OrderId = orderId;
        CustomerId = customerId;
        TotalAmount = totalAmount;
    }
}
```

### 7. Domain Event Handlers

Using MediatR for domain event notifications:

```csharp
using MediatR;

// Event handler
public class CustomerRegisteredEventHandler : INotificationHandler<CustomerRegisteredEvent>
{
    private readonly IEmailService _emailService;

    public CustomerRegisteredEventHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(CustomerRegisteredEvent notification, CancellationToken ct)
    {
        await _emailService.SendWelcomeEmailAsync(
            notification.Email,
            notification.CustomerId,
            ct);
    }
}

public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
{
    private readonly IInventoryService _inventoryService;

    public async Task Handle(OrderPlacedEvent notification, CancellationToken ct)
    {
        await _inventoryService.ReserveInventoryAsync(notification.OrderId, ct);
    }
}
```

### 8. Domain Event Dispatcher

MediatR-based domain event dispatcher:

```csharp
public class MediatRDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public MediatRDomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAndClearEvents(IEnumerable<HasDomainEventsBase> entitiesWithEvents)
    {
        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToArray();
            entity.ClearDomainEvents();
            
            foreach (var domainEvent in events)
            {
                await _mediator.Publish(domainEvent).ConfigureAwait(false);
            }
        }
    }
}
```

**Setup in EF Core:**

```csharp
public class AppDbContext : DbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IDomainEventDispatcher dispatcher) : base(options)
    {
        _dispatcher = dispatcher;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Get entities with domain events
        var entitiesWithEvents = ChangeTracker.Entries<HasDomainEventsBase>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .ToArray();

        // Save changes first
        var result = await base.SaveChangesAsync(ct);

        // Then dispatch events
        await _dispatcher.DispatchAndClearEvents(entitiesWithEvents);

        return result;
    }
}
```

## Repository Interfaces

### IReadRepository<T>

Read-only repository operations:

```csharp
public interface IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T?> GetByIdAsync<TId>(TId id, CancellationToken ct = default) where TId : notnull;
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<List<T>> ListAsync(CancellationToken ct = default);
    Task<List<T>> ListAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(ISpecification<T> spec, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
}
```

### IRepository<T>

Full repository with write operations:

```csharp
public interface IRepository<T> : IReadRepository<T> where T : class, IAggregateRoot
{
    Task<T> AddAsync(T entity, CancellationToken ct = default);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task DeleteRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
```

### Implementation with EF Core

```csharp
using Ardalis.Specification.EntityFrameworkCore;

public class EfRepository<T> : RepositoryBase<T>, IRepository<T> 
    where T : class, IAggregateRoot
{
    public EfRepository(AppDbContext dbContext) : base(dbContext)
    {
    }
}

// Registration
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
```

### Usage in Services

```csharp
public class CustomerService
{
    private readonly IRepository<Customer> _customerRepository;

    public CustomerService(IRepository<Customer> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Customer> CreateCustomerAsync(
        string firstName, 
        string lastName, 
        string email)
    {
        var customer = new Customer(firstName, lastName, email);
        await _customerRepository.AddAsync(customer);
        await _customerRepository.SaveChangesAsync();
        return customer;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        return await _customerRepository.GetByIdAsync(id);
    }
}

public class OrderQueryService
{
    private readonly IReadRepository<Order> _orderRepository;

    public OrderQueryService(IReadRepository<Order> orderRepository)
    {
        _orderRepository = orderRepository; // Read-only intent
    }

    public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
    {
        var spec = new OrdersByCustomerSpec(customerId);
        return await _orderRepository.ListAsync(spec);
    }
}
```

## CQRS Base Classes

### Commands

```csharp
public interface ICommand : IRequest<Result>
{
}

public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

// Usage
public record CreateCustomerCommand(
    string FirstName,
    string LastName,
    string Email) : ICommand<Result<int>>;

public record UpdateCustomerCommand(
    int CustomerId,
    string FirstName,
    string LastName) : ICommand<Result>;

public record DeleteCustomerCommand(int CustomerId) : ICommand<Result>;
```

### Command Handlers

```csharp
public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
}

// Usage
public class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, Result<int>>
{
    private readonly IRepository<Customer> _repository;

    public CreateCustomerHandler(IRepository<Customer> repository)
    {
        _repository = repository;
    }

    public async Task<Result<int>> Handle(
        CreateCustomerCommand command, 
        CancellationToken ct)
    {
        var customer = new Customer(
            command.FirstName,
            command.LastName,
            command.Email);

        await _repository.AddAsync(customer, ct);
        await _repository.SaveChangesAsync(ct);

        return Result<int>.Success(customer.Id);
    }
}
```

### Queries

```csharp
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

// Usage
public record GetCustomerQuery(int CustomerId) : IQuery<Result<CustomerDto>>;

public record GetCustomersQuery(
    int Page,
    int PageSize) : IQuery<Result<PagedResult<CustomerDto>>>;
```

### Query Handlers

```csharp
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}

// Usage
public class GetCustomerHandler : IQueryHandler<GetCustomerQuery, Result<CustomerDto>>
{
    private readonly IReadRepository<Customer> _repository;

    public GetCustomerHandler(IReadRepository<Customer> repository)
    {
        _repository = repository;
    }

    public async Task<Result<CustomerDto>> Handle(
        GetCustomerQuery query,
        CancellationToken ct)
    {
        var customer = await _repository.GetByIdAsync(query.CustomerId, ct);
        
        if (customer == null)
            return Result<CustomerDto>.NotFound();

        var dto = new CustomerDto
        {
            Id = customer.Id,
            FullName = $"{customer.FirstName} {customer.LastName}",
            Email = customer.Email
        };

        return Result<CustomerDto>.Success(dto);
    }
}
```

## MediatR Integration

### Setup

```bash
dotnet add package MediatR
```

```csharp
// Program.cs
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
```

### Logging Behavior

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        
        var response = await next();
        
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        
        return response;
    }
}

// Registration
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
});
```

## Creating Your Own SharedKernel

**Recommended approach** - Don't use Ardalis.SharedKernel directly in production!

### 1. Create YourCompany.SharedKernel Project

```bash
mkdir YourCompany.SharedKernel
cd YourCompany.SharedKernel
dotnet new classlib
```

### 2. Copy Base Classes

Copy and customize these classes from Ardalis.SharedKernel:
- EntityBase / EntityBase<TId>
- HasDomainEventsBase
- ValueObject
- DomainEventBase
- IAggregateRoot
- IRepository / IReadRepository
- ICommand / IQuery interfaces
- ICommandHandler / IQueryHandler interfaces

### 3. Add Your Custom Types

```csharp
// YourCompany.SharedKernel/AuditableEntityBase.cs
public abstract class AuditableEntityBase : HasDomainEventsBase, IAggregateRoot
{
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
}

// YourCompany.SharedKernel/SoftDeletableEntityBase.cs
public abstract class SoftDeletableEntityBase : AuditableEntityBase
{
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

### 4. Package and Publish

```xml
<!-- YourCompany.SharedKernel.csproj -->
<PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <PackageId>YourCompany.SharedKernel</PackageId>
    <Version>1.0.0</Version>
    <Authors>Your Team</Authors>
    <Company>Your Company</Company>
</PropertyGroup>
```

```bash
dotnet pack
# Publish to your private NuGet feed
```

## Best Practices

### 1. Use Aggregate Roots Correctly

```csharp
// ✅ Good - Order is aggregate root
public class Order : HasDomainEventsBase, IAggregateRoot
{
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    
    public void AddItem(Product product, int quantity)
    {
        // Aggregate root controls children
        var item = new OrderItem(product.Id, quantity, product.Price);
        _items.Add(item);
    }
}

// ❌ Bad - OrderItem accessed directly
public class OrderItem : EntityBase, IAggregateRoot // Wrong!
{
}
```

### 2. Make Value Objects Immutable

```csharp
// ✅ Good - immutable
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    
    public Money Add(Money other) => new Money(Amount + other.Amount, Currency);
}

// ❌ Bad - mutable
public class Money : ValueObject
{
    public decimal Amount { get; set; } // Should be private set!
}
```

### 3. Register Domain Events Inside Aggregate

```csharp
// ✅ Good - event raised inside aggregate
public class Order : HasDomainEventsBase, IAggregateRoot
{
    public void Submit()
    {
        Status = OrderStatus.Submitted;
        RegisterDomainEvent(new OrderSubmittedEvent(this));
    }
}

// ❌ Bad - event raised outside aggregate
order.Submit();
order.RegisterDomainEvent(new OrderSubmittedEvent(order)); // Wrong!
```

### 4. Use Read Repository for Queries

```csharp
// ✅ Good - read-only intent
public class OrderQueryService
{
    private readonly IReadRepository<Order> _repository;
}

// ❌ Bad - write repository for read operations
public class OrderQueryService
{
    private readonly IRepository<Order> _repository; // Can accidentally modify!
}
```

### 5. Separate Commands and Queries

```csharp
// ✅ Good - CQRS separation
public record CreateOrderCommand(...) : ICommand<Result<int>>;
public record GetOrderQuery(...) : IQuery<Result<OrderDto>>;

// ❌ Bad - mixed responsibilities
public record OrderRequest(...) : IRequest<Result>; // What does it do?
```

## Common Patterns

See references/examples.md for complete patterns including:
- Aggregate with domain events
- Value object collections
- CQRS with MediatR
- Repository with specifications
- Domain event workflows

## References

- GitHub: https://github.com/ardalis/Ardalis.SharedKernel
- NuGet: https://www.nuget.org/packages/Ardalis.SharedKernel
- Clean Architecture Template: https://github.com/ardalis/CleanArchitecture
- Latest Version: 5.0.0+
- License: MIT
- DDD Course: https://www.pluralsight.com/courses/domain-driven-design-fundamentals
