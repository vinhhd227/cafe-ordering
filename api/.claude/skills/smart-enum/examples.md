# SmartEnum Examples Reference

This file contains additional examples and patterns for using Ardalis.SmartEnum.

## Complete Domain-Driven Design Example

```csharp
using Ardalis.SmartEnum;

// Value Object as SmartEnum
public sealed class Currency : SmartEnum<Currency, string>
{
    public static readonly Currency USD = new("USD", "USD", "$", "US Dollar", 2);
    public static readonly Currency EUR = new("EUR", "EUR", "€", "Euro", 2);
    public static readonly Currency JPY = new("JPY", "JPY", "¥", "Japanese Yen", 0);
    public static readonly Currency GBP = new("GBP", "GBP", "£", "British Pound", 2);

    public string Symbol { get; }
    public string DisplayName { get; }
    public int DecimalPlaces { get; }

    private Currency(string name, string value, string symbol, string displayName, int decimalPlaces) 
        : base(name, value)
    {
        Symbol = symbol;
        DisplayName = displayName;
        DecimalPlaces = decimalPlaces;
    }

    public string Format(decimal amount)
    {
        return $"{Symbol}{Math.Round(amount, DecimalPlaces):N" + DecimalPlaces + "}";
    }
}

// Usage
var price = 1234.56m;
var formatted = Currency.USD.Format(price); // "$1,234.56"
```

## State Machine Pattern

```csharp
using Ardalis.SmartEnum;

public abstract class DocumentState : SmartEnum<DocumentState>
{
    public static readonly DocumentState Draft = new DraftState();
    public static readonly DocumentState UnderReview = new UnderReviewState();
    public static readonly DocumentState Approved = new ApprovedState();
    public static readonly DocumentState Published = new PublishedState();
    public static readonly DocumentState Archived = new ArchivedState();

    private DocumentState(string name, int value) : base(name, value)
    {
    }

    public abstract bool CanTransitionTo(DocumentState targetState);
    public abstract IEnumerable<DocumentState> AllowedTransitions { get; }
    public abstract bool CanEdit { get; }
    public abstract bool CanDelete { get; }

    private sealed class DraftState : DocumentState
    {
        public DraftState() : base("Draft", 1) { }
        public override bool CanEdit => true;
        public override bool CanDelete => true;
        public override IEnumerable<DocumentState> AllowedTransitions => 
            new[] { UnderReview, Archived };
        public override bool CanTransitionTo(DocumentState targetState) => 
            targetState == UnderReview || targetState == Archived;
    }

    private sealed class UnderReviewState : DocumentState
    {
        public UnderReviewState() : base("UnderReview", 2) { }
        public override bool CanEdit => false;
        public override bool CanDelete => false;
        public override IEnumerable<DocumentState> AllowedTransitions => 
            new[] { Draft, Approved, Archived };
        public override bool CanTransitionTo(DocumentState targetState) => 
            targetState == Draft || targetState == Approved || targetState == Archived;
    }

    private sealed class ApprovedState : DocumentState
    {
        public ApprovedState() : base("Approved", 3) { }
        public override bool CanEdit => false;
        public override bool CanDelete => false;
        public override IEnumerable<DocumentState> AllowedTransitions => 
            new[] { Published, Archived };
        public override bool CanTransitionTo(DocumentState targetState) => 
            targetState == Published || targetState == Archived;
    }

    private sealed class PublishedState : DocumentState
    {
        public PublishedState() : base("Published", 4) { }
        public override bool CanEdit => false;
        public override bool CanDelete => false;
        public override IEnumerable<DocumentState> AllowedTransitions => 
            new[] { Archived };
        public override bool CanTransitionTo(DocumentState targetState) => 
            targetState == Archived;
    }

    private sealed class ArchivedState : DocumentState
    {
        public ArchivedState() : base("Archived", 5) { }
        public override bool CanEdit => false;
        public override bool CanDelete => true;
        public override IEnumerable<DocumentState> AllowedTransitions => Array.Empty<DocumentState>();
        public override bool CanTransitionTo(DocumentState targetState) => false;
    }
}

// Usage
public class Document
{
    public DocumentState State { get; private set; } = DocumentState.Draft;

    public void TransitionTo(DocumentState newState)
    {
        if (!State.CanTransitionTo(newState))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {State.Name} to {newState.Name}");
        }
        State = newState;
    }

    public void Edit()
    {
        if (!State.CanEdit)
        {
            throw new InvalidOperationException(
                $"Cannot edit document in {State.Name} state");
        }
        // Edit logic
    }
}
```

## Complex SmartFlagEnum with Permissions

```csharp
using Ardalis.SmartEnum;

public abstract class Permission : SmartFlagEnum<Permission>
{
    public static readonly Permission None = new NonePermission();
    public static readonly Permission Read = new ReadPermission();
    public static readonly Permission Write = new WritePermission();
    public static readonly Permission Delete = new DeletePermission();
    public static readonly Permission Admin = new AdminPermission();

    private Permission(string name, int value) : base(name, value)
    {
    }

    public abstract string Description { get; }
    public abstract PermissionLevel Level { get; }

    private sealed class NonePermission : Permission
    {
        public NonePermission() : base("None", 0) { }
        public override string Description => "No permissions";
        public override PermissionLevel Level => PermissionLevel.None;
    }

    private sealed class ReadPermission : Permission
    {
        public ReadPermission() : base("Read", 1) { }
        public override string Description => "View content";
        public override PermissionLevel Level => PermissionLevel.Basic;
    }

    private sealed class WritePermission : Permission
    {
        public WritePermission() : base("Write", 2) { }
        public override string Description => "Create and modify content";
        public override PermissionLevel Level => PermissionLevel.Standard;
    }

    private sealed class DeletePermission : Permission
    {
        public DeletePermission() : base("Delete", 4) { }
        public override string Description => "Remove content";
        public override PermissionLevel Level => PermissionLevel.Elevated;
    }

    private sealed class AdminPermission : Permission
    {
        public AdminPermission() : base("Admin", 8) { }
        public override string Description => "Full administrative access";
        public override PermissionLevel Level => PermissionLevel.Administrator;
    }
}

public enum PermissionLevel
{
    None,
    Basic,
    Standard,
    Elevated,
    Administrator
}

// Usage
public class User
{
    public IEnumerable<Permission> Permissions { get; set; }

    public bool HasPermission(Permission permission)
    {
        return Permissions.Contains(permission);
    }

    public bool HasAllPermissions(params Permission[] requiredPermissions)
    {
        return requiredPermissions.All(p => Permissions.Contains(p));
    }

    public bool HasAnyPermission(params Permission[] requiredPermissions)
    {
        return requiredPermissions.Any(p => Permissions.Contains(p));
    }
}

// Example usage
var user = new User
{
    Permissions = Permission.FromValue(3) // Read + Write
};

Console.WriteLine(user.HasPermission(Permission.Read));   // True
Console.WriteLine(user.HasPermission(Permission.Delete)); // False
Console.WriteLine(user.HasAllPermissions(Permission.Read, Permission.Write)); // True
```

## ASP.NET Core Model Binding

```csharp
using Ardalis.SmartEnum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

// Custom Model Binder
public class SmartEnumModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var modelName = bindingContext.ModelName;
        var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        var value = valueProviderResult.FirstValue;

        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        var modelType = bindingContext.ModelType;
        var fromNameMethod = modelType.GetMethod(
            "FromName", 
            new[] { typeof(string), typeof(bool) }
        );

        try
        {
            var result = fromNameMethod?.Invoke(null, new object[] { value, true });
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch
        {
            bindingContext.ModelState.AddModelError(
                modelName, 
                $"Invalid value '{value}' for {modelType.Name}"
            );
        }

        return Task.CompletedTask;
    }
}

// Controller usage
public class CreateProductRequest
{
    public string Name { get; set; }
    
    [ModelBinder(BinderType = typeof(SmartEnumModelBinder))]
    public ProductCategory Category { get; set; }
}

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    [HttpPost]
    public IActionResult Create([FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Use request.Category as ProductCategory SmartEnum
        return Ok();
    }
}
```

## Minimal API with SmartEnum

```csharp
using Ardalis.SmartEnum;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Bind SmartEnum from route parameter
app.MapGet("/orders/{status}", (string status) =>
{
    if (!OrderStatus.TryFromName(status, ignoreCase: true, out var orderStatus))
    {
        return Results.BadRequest($"Invalid status: {status}");
    }

    // Use orderStatus
    return Results.Ok(new { Status = orderStatus.Name, Value = orderStatus.Value });
});

// Bind SmartEnum from query parameter
app.MapGet("/products", (string? category) =>
{
    if (category is null)
    {
        return Results.Ok("All products");
    }

    if (!ProductCategory.TryFromName(category, ignoreCase: true, out var categoryEnum))
    {
        return Results.BadRequest($"Invalid category: {category}");
    }

    return Results.Ok($"Products in {categoryEnum.Name}");
});

app.Run();
```

## Repository Pattern with Specification

```csharp
using Ardalis.SmartEnum;
using System.Linq.Expressions;

// SmartEnum for entity status
public sealed class EntityStatus : SmartEnum<EntityStatus>
{
    public static readonly EntityStatus Active = new(nameof(Active), 1);
    public static readonly EntityStatus Inactive = new(nameof(Inactive), 2);
    public static readonly EntityStatus Deleted = new(nameof(Deleted), 3);

    private EntityStatus(string name, int value) : base(name, value)
    {
    }
}

// Specification pattern
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
}

public class EntityStatusSpecification<T> : ISpecification<T> 
    where T : IHaveStatus
{
    private readonly EntityStatus _status;

    public EntityStatusSpecification(EntityStatus status)
    {
        _status = status;
    }

    public Expression<Func<T, bool>> Criteria => 
        entity => entity.Status == _status;
}

public interface IHaveStatus
{
    EntityStatus Status { get; }
}

// Repository
public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAsync(ISpecification<T> specification);
}

// Usage
public class Product : IHaveStatus
{
    public int Id { get; set; }
    public string Name { get; set; }
    public EntityStatus Status { get; set; }
}

public class ProductService
{
    private readonly IRepository<Product> _repository;

    public ProductService(IRepository<Product> repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        var spec = new EntityStatusSpecification<Product>(EntityStatus.Active);
        return await _repository.GetAsync(spec);
    }
}
```

## Unit Testing Patterns

```csharp
using Ardalis.SmartEnum;
using Xunit;
using FluentAssertions;

public class SmartEnumTests
{
    [Fact]
    public void FromName_WithValidName_ReturnsCorrectInstance()
    {
        // Arrange & Act
        var status = OrderStatus.FromName("Pending");

        // Assert
        status.Should().Be(OrderStatus.Pending);
        status.Value.Should().Be(1);
    }

    [Theory]
    [InlineData("pending", true)]
    [InlineData("PENDING", true)]
    [InlineData("Pending", true)]
    public void FromName_WithIgnoreCase_ReturnsCorrectInstance(string name, bool ignoreCase)
    {
        // Act
        var status = OrderStatus.FromName(name, ignoreCase);

        // Assert
        status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void FromName_WithInvalidName_ThrowsException()
    {
        // Act & Assert
        Action act = () => OrderStatus.FromName("Invalid");
        act.Should().Throw<SmartEnumNotFoundException>();
    }

    [Fact]
    public void TryFromName_WithInvalidName_ReturnsFalse()
    {
        // Act
        var result = OrderStatus.TryFromName("Invalid", out var status);

        // Assert
        result.Should().BeFalse();
        status.Should().BeNull();
    }

    [Fact]
    public void FromValue_WithValidValue_ReturnsCorrectInstance()
    {
        // Act
        var status = OrderStatus.FromValue(1);

        // Assert
        status.Should().Be(OrderStatus.Pending);
    }

    [Theory]
    [MemberData(nameof(GetAllOrderStatuses))]
    public void List_ContainsAllStatuses(OrderStatus expectedStatus)
    {
        // Assert
        OrderStatus.List.Should().Contain(expectedStatus);
    }

    public static IEnumerable<object[]> GetAllOrderStatuses()
    {
        return OrderStatus.List.Select(s => new object[] { s });
    }

    [Fact]
    public void Equality_SameValue_ReturnsTrue()
    {
        // Arrange
        var status1 = OrderStatus.FromValue(1);
        var status2 = OrderStatus.FromName("Pending");

        // Assert
        (status1 == status2).Should().BeTrue();
        status1.Equals(status2).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_DifferentValues_ReturnsCorrectOrder()
    {
        // Arrange
        var pending = OrderStatus.Pending;
        var processing = OrderStatus.Processing;

        // Assert
        (pending < processing).Should().BeTrue();
        pending.CompareTo(processing).Should().BeLessThan(0);
    }
}

// Testing SmartEnum with behavior
public class EmployeeTypeTests
{
    [Theory]
    [InlineData("Manager", 10_000)]
    [InlineData("Director", 100_000)]
    [InlineData("Assistant", 1_000)]
    public void BonusSize_ReturnsCorrectAmount(string typeName, decimal expectedBonus)
    {
        // Arrange
        var employeeType = EmployeeType.FromName(typeName);

        // Assert
        employeeType.BonusSize.Should().Be(expectedBonus);
    }
}
```

## MediatR Integration

```csharp
using Ardalis.SmartEnum;
using MediatR;

// Command with SmartEnum
public class UpdateOrderStatusCommand : IRequest<Result>
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
}

// Handler
public class UpdateOrderStatusCommandHandler 
    : IRequestHandler<UpdateOrderStatusCommand, Result>
{
    private readonly IOrderRepository _repository;

    public UpdateOrderStatusCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(
        UpdateOrderStatusCommand request, 
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        
        if (order == null)
        {
            return Result.NotFound();
        }

        // Use SmartEnum behavior
        if (!order.Status.CanTransitionTo(request.NewStatus))
        {
            return Result.Invalid($"Cannot transition from {order.Status.Name} to {request.NewStatus.Name}");
        }

        order.UpdateStatus(request.NewStatus);
        await _repository.SaveChangesAsync();

        return Result.Success();
    }
}

public class Result
{
    public bool IsSuccess { get; set; }
    public string Error { get; set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result NotFound() => new() { IsSuccess = false, Error = "Not found" };
    public static Result Invalid(string error) => new() { IsSuccess = false, Error = error };
}
```

## Strongly-Typed IDs with SmartEnum

```csharp
using Ardalis.SmartEnum;

// Using SmartEnum for strongly-typed identifiers
public abstract class EntityId<TEntity> : SmartEnum<EntityId<TEntity>, Guid>
{
    protected EntityId(string name, Guid value) : base(name, value)
    {
    }

    public static EntityId<TEntity> New() => 
        CreateInstance(Guid.NewGuid().ToString(), Guid.NewGuid());

    protected static EntityId<TEntity> CreateInstance(string name, Guid value)
    {
        var instance = Activator.CreateInstance(
            typeof(TEntity).Assembly
                .GetTypes()
                .First(t => t.BaseType == typeof(EntityId<TEntity>)),
            new object[] { name, value }
        );
        return (EntityId<TEntity>)instance;
    }
}

// Usage
public class OrderId : EntityId<Order>
{
    private OrderId(string name, Guid value) : base(name, value)
    {
    }
}

public class Order
{
    public OrderId Id { get; private set; }
    public string CustomerName { get; set; }

    public Order()
    {
        Id = OrderId.New();
    }
}
```

## Caching SmartEnum Lookups

```csharp
using Ardalis.SmartEnum;
using Microsoft.Extensions.Caching.Memory;

public class SmartEnumCache
{
    private readonly IMemoryCache _cache;

    public SmartEnumCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public TEnum GetByName<TEnum>(string name) where TEnum : SmartEnum<TEnum>
    {
        var cacheKey = $"{typeof(TEnum).Name}_{name}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return SmartEnum<TEnum>.FromName(name);
        });
    }

    public TEnum GetByValue<TEnum>(int value) where TEnum : SmartEnum<TEnum>
    {
        var cacheKey = $"{typeof(TEnum).Name}_{value}";
        
        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return SmartEnum<TEnum>.FromValue(value);
        });
    }
}
```

## Migration from Traditional Enum

```csharp
// Before: Traditional enum
public enum OldOrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4
}

// After: SmartEnum
public sealed class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Pending = new(nameof(Pending), 1);
    public static readonly OrderStatus Processing = new(nameof(Processing), 2);
    public static readonly OrderStatus Shipped = new(nameof(Shipped), 3);
    public static readonly OrderStatus Delivered = new(nameof(Delivered), 4);

    private OrderStatus(string name, int value) : base(name, value)
    {
    }

    // Migration helper
    public static OrderStatus FromOldEnum(OldOrderStatus oldStatus)
    {
        return FromValue((int)oldStatus);
    }

    public OldOrderStatus ToOldEnum()
    {
        return (OldOrderStatus)Value;
    }
}

// Migration strategy
public class OrderMigrationService
{
    public void MigrateOrders()
    {
        // Read old orders
        var oldOrders = GetOldOrders();

        foreach (var oldOrder in oldOrders)
        {
            var newStatus = OrderStatus.FromOldEnum(oldOrder.Status);
            // Save with new status
            SaveOrder(oldOrder.Id, newStatus);
        }
    }

    private IEnumerable<OldOrder> GetOldOrders() => throw new NotImplementedException();
    private void SaveOrder(int id, OrderStatus status) => throw new NotImplementedException();
}

public class OldOrder
{
    public int Id { get; set; }
    public OldOrderStatus Status { get; set; }
}
```
