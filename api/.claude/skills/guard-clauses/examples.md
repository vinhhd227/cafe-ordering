# GuardClauses Advanced Examples

Advanced patterns and real-world examples for using Ardalis.GuardClauses.

## Domain-Driven Design Examples

### Aggregate Root with Guards

```csharp
using Ardalis.GuardClauses;

public class Order
{
    private readonly List<OrderItem> _items = new();

    public int Id { get; private set; }
    public string CustomerName { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.SubTotal);

    public Order(int id, string customerName)
    {
        Id = Guard.Against.NegativeOrZero(id, nameof(id));
        CustomerName = Guard.Against.NullOrWhiteSpace(customerName, nameof(customerName));
        Status = OrderStatus.Pending;
    }

    public void AddItem(string productName, int quantity, decimal unitPrice)
    {
        Guard.Against.NullOrWhiteSpace(productName, nameof(productName));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.Negative(unitPrice, nameof(unitPrice));

        var item = new OrderItem(productName, quantity, unitPrice);
        _items.Add(item);
    }

    public void RemoveItem(string productName)
    {
        Guard.Against.NullOrWhiteSpace(productName, nameof(productName));

        var item = _items.FirstOrDefault(i => i.ProductName == productName);
        Guard.Against.NotFound(productName, item);

        _items.Remove(item);
    }

    public void ChangeStatus(OrderStatus newStatus)
    {
        Guard.Against.EnumOutOfRange(newStatus, nameof(newStatus));

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition from {Status} to {newStatus}"
            );
        }

        Status = newStatus;
    }

    private bool CanTransitionTo(OrderStatus targetStatus)
    {
        return (Status, targetStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
    }
}

public class OrderItem
{
    public string ProductName { get; }
    public int Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal SubTotal => Quantity * UnitPrice;

    public OrderItem(string productName, int quantity, decimal unitPrice)
    {
        ProductName = Guard.Against.NullOrWhiteSpace(productName);
        Quantity = Guard.Against.NegativeOrZero(quantity);
        UnitPrice = Guard.Against.Negative(unitPrice);
    }
}

public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4
}
```

### Value Objects with Guards

```csharp
using Ardalis.GuardClauses;

public class Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    public Email(string value)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(value, nameof(value)).Trim().ToLower();
        
        Guard.Against.InvalidFormat(
            normalized,
            nameof(value),
            EmailRegex.IsMatch,
            "Invalid email format"
        );

        Value = normalized;
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is Email email && Value == email.Value;
    public override int GetHashCode() => Value.GetHashCode();
}

public class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = Guard.Against.Negative(amount, nameof(amount));
        Currency = Guard.Against.NullOrWhiteSpace(currency, nameof(currency)).ToUpper();

        Guard.Against.InvalidFormat(
            Currency,
            nameof(currency),
            c => c.Length == 3 && c.All(char.IsLetter),
            "Currency must be 3-letter ISO code"
        );
    }

    public Money Add(Money other)
    {
        Guard.Against.Null(other, nameof(other));

        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                "Cannot add money with different currencies"
            );
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

public class PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        var cleaned = Guard.Against.NullOrWhiteSpace(value, nameof(value))
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "");

        Guard.Against.InvalidFormat(
            cleaned,
            nameof(value),
            phone => phone.Length >= 10 && phone.All(char.IsDigit),
            "Phone number must contain at least 10 digits"
        );

        Value = cleaned;
    }

    public string Formatted => Value.Length == 10
        ? $"({Value[..3]}) {Value[3..6]}-{Value[6..]}"
        : Value;

    public static implicit operator string(PhoneNumber phone) => phone.Value;
    public static explicit operator PhoneNumber(string value) => new(value);
}
```

## CQRS Pattern with Guards

### Commands

```csharp
using Ardalis.GuardClauses;

public class CreateOrderCommand
{
    public string CustomerName { get; }
    public List<OrderItemDto> Items { get; }

    public CreateOrderCommand(string customerName, List<OrderItemDto> items)
    {
        CustomerName = Guard.Against.NullOrWhiteSpace(customerName);
        Items = Guard.Against.NullOrEmpty(items);

        foreach (var item in Items)
        {
            Guard.Against.Null(item);
            Guard.Against.NullOrWhiteSpace(item.ProductName);
            Guard.Against.NegativeOrZero(item.Quantity);
            Guard.Against.Negative(item.UnitPrice);
        }
    }
}

public class UpdateOrderStatusCommand
{
    public int OrderId { get; }
    public OrderStatus NewStatus { get; }

    public UpdateOrderStatusCommand(int orderId, OrderStatus newStatus)
    {
        OrderId = Guard.Against.NegativeOrZero(orderId);
        NewStatus = Guard.Against.EnumOutOfRange(newStatus);
    }
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
```

### Command Handlers

```csharp
using Ardalis.GuardClauses;

public class CreateOrderCommandHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository)
    {
        _orderRepository = Guard.Against.Null(orderRepository);
        _customerRepository = Guard.Against.Null(customerRepository);
    }

    public async Task<OrderResult> HandleAsync(CreateOrderCommand command)
    {
        Guard.Against.Null(command, nameof(command));

        var customer = await _customerRepository.GetByNameAsync(command.CustomerName);
        Guard.Against.NotFound(command.CustomerName, customer);

        var order = new Order(GenerateOrderId(), customer.Name);

        foreach (var item in command.Items)
        {
            order.AddItem(item.ProductName, item.Quantity, item.UnitPrice);
        }

        await _orderRepository.AddAsync(order);

        return new OrderResult(order.Id, order.Total);
    }

    private int GenerateOrderId() => Random.Shared.Next(1, int.MaxValue);
}
```

### Queries

```csharp
using Ardalis.GuardClauses;

public class GetOrderByIdQuery
{
    public int OrderId { get; }

    public GetOrderByIdQuery(int orderId)
    {
        OrderId = Guard.Against.NegativeOrZero(orderId);
    }
}

public class GetOrdersByStatusQuery
{
    public OrderStatus Status { get; }
    public int Page { get; }
    public int PageSize { get; }

    public GetOrdersByStatusQuery(OrderStatus status, int page = 1, int pageSize = 10)
    {
        Status = Guard.Against.EnumOutOfRange(status);
        Page = Guard.Against.NegativeOrZero(page);
        PageSize = Guard.Against.OutOfRange(pageSize, nameof(pageSize), 1, 100);
    }
}
```

## ASP.NET Core Integration

### Controller with Guards

```csharp
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CreateOrderCommandHandler _createOrderHandler;
    private readonly IOrderRepository _orderRepository;

    public OrdersController(
        CreateOrderCommandHandler createOrderHandler,
        IOrderRepository orderRepository)
    {
        _createOrderHandler = Guard.Against.Null(createOrderHandler);
        _orderRepository = Guard.Against.Null(orderRepository);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResult>> CreateOrder(CreateOrderRequest request)
    {
        Guard.Against.Null(request, nameof(request));

        try
        {
            var command = new CreateOrderCommand(
                request.CustomerName,
                request.Items
            );

            var result = await _createOrderHandler.HandleAsync(command);
            return CreatedAtAction(nameof(GetOrder), new { id = result.OrderId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        try
        {
            Guard.Against.NegativeOrZero(id, nameof(id));

            var order = await _orderRepository.GetByIdAsync(id);
            Guard.Against.NotFound(id, order);

            return Ok(MapToDto(order));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            Guard.Against.NegativeOrZero(id, nameof(id));
            Guard.Against.Null(request, nameof(request));

            var order = await _orderRepository.GetByIdAsync(id);
            Guard.Against.NotFound(id, order);

            order.ChangeStatus(request.NewStatus);
            await _orderRepository.UpdateAsync(order);

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        CustomerName = order.CustomerName,
        Status = order.Status.ToString(),
        Total = order.Total,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductName = i.ProductName,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList()
    };
}

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class UpdateStatusRequest
{
    public OrderStatus NewStatus { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderResult
{
    public int OrderId { get; }
    public decimal Total { get; }

    public OrderResult(int orderId, decimal total)
    {
        OrderId = orderId;
        Total = total;
    }
}
```

### Minimal API with Guards

```csharp
using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/orders", async (
    CreateOrderRequest request,
    CreateOrderCommandHandler handler) =>
{
    try
    {
        Guard.Against.Null(request, nameof(request));

        var command = new CreateOrderCommand(
            request.CustomerName,
            request.Items
        );

        var result = await handler.HandleAsync(command);
        return Results.Created($"/api/orders/{result.OrderId}", result);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (NotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapGet("/api/orders/{id:int}", async (
    int id,
    IOrderRepository repository) =>
{
    try
    {
        Guard.Against.NegativeOrZero(id, nameof(id));

        var order = await repository.GetByIdAsync(id);
        Guard.Against.NotFound(id, order);

        return Results.Ok(order);
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (NotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.Run();
```

## Repository Pattern Examples

### Generic Repository with Guards

```csharp
using Ardalis.GuardClauses;

public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;

    public Repository(DbContext context)
    {
        _context = Guard.Against.Null(context, nameof(context));
    }

    public async Task<T> GetByIdAsync(int id)
    {
        Guard.Against.NegativeOrZero(id, nameof(id));

        var entity = await _context.Set<T>().FindAsync(id);
        Guard.Against.NotFound(id, entity);

        return entity;
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        Guard.Against.Null(entity, nameof(entity));

        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        Guard.Against.Null(entity, nameof(entity));

        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        Guard.Against.NegativeOrZero(id, nameof(id));

        var entity = await GetByIdAsync(id);
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }
}
```

### Specification Pattern with Guards

```csharp
using Ardalis.GuardClauses;

public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
}

public class OrdersByStatusSpecification : ISpecification<Order>
{
    private readonly OrderStatus _status;

    public OrdersByStatusSpecification(OrderStatus status)
    {
        _status = Guard.Against.EnumOutOfRange(status, nameof(status));
    }

    public bool IsSatisfiedBy(Order entity)
    {
        Guard.Against.Null(entity, nameof(entity));
        return entity.Status == _status;
    }
}

public class OrdersByDateRangeSpecification : ISpecification<Order>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public OrdersByDateRangeSpecification(DateTime startDate, DateTime endDate)
    {
        _startDate = Guard.Against.OutOfSQLDateRange(startDate, nameof(startDate));
        _endDate = Guard.Against.OutOfSQLDateRange(endDate, nameof(endDate));

        Guard.Against.Expression(
            _ => _endDate < _startDate,
            _endDate,
            "End date must be after start date"
        );
    }

    public bool IsSatisfiedBy(Order entity)
    {
        Guard.Against.Null(entity, nameof(entity));
        return entity.CreatedDate >= _startDate && entity.CreatedDate <= _endDate;
    }
}
```

## Advanced Custom Guards

### Range Guards

```csharp
namespace Ardalis.GuardClauses
{
    public static class RangeGuards
    {
        public static decimal Between(
            this IGuardClause guardClause,
            decimal input,
            decimal min,
            decimal max,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            if (input < min || input > max)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    $"Value must be between {min} and {max}, but was {input}"
                );
            }

            return input;
        }

        public static int GreaterThan(
            this IGuardClause guardClause,
            int input,
            int threshold,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            if (input <= threshold)
            {
                throw new ArgumentException(
                    $"Value must be greater than {threshold}, but was {input}",
                    parameterName
                );
            }

            return input;
        }

        public static int LessThan(
            this IGuardClause guardClause,
            int input,
            int threshold,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            if (input >= threshold)
            {
                throw new ArgumentException(
                    $"Value must be less than {threshold}, but was {input}",
                    parameterName
                );
            }

            return input;
        }
    }
}
```

### Collection Guards

```csharp
namespace Ardalis.GuardClauses
{
    public static class CollectionGuards
    {
        public static IEnumerable<T> Empty<T>(
            this IGuardClause guardClause,
            IEnumerable<T> input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.Null(input, parameterName);

            if (!input.Any())
            {
                throw new ArgumentException(
                    "Collection cannot be empty",
                    parameterName
                );
            }

            return input;
        }

        public static ICollection<T> MaxCount<T>(
            this IGuardClause guardClause,
            ICollection<T> input,
            int maxCount,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count > maxCount)
            {
                throw new ArgumentException(
                    $"Collection cannot contain more than {maxCount} items, but has {input.Count}",
                    parameterName
                );
            }

            return input;
        }

        public static ICollection<T> MinCount<T>(
            this IGuardClause guardClause,
            ICollection<T> input,
            int minCount,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count < minCount)
            {
                throw new ArgumentException(
                    $"Collection must contain at least {minCount} items, but has {input.Count}",
                    parameterName
                );
            }

            return input;
        }

        public static IEnumerable<T> Duplicates<T>(
            this IGuardClause guardClause,
            IEnumerable<T> input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.Null(input, parameterName);

            var list = input.ToList();
            var duplicates = list.GroupBy(x => x)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Any())
            {
                throw new ArgumentException(
                    $"Collection contains duplicate values: {string.Join(", ", duplicates)}",
                    parameterName
                );
            }

            return input;
        }
    }
}
```

### String Guards

```csharp
namespace Ardalis.GuardClauses
{
    public static class StringGuards
    {
        public static string MaxLength(
            this IGuardClause guardClause,
            string input,
            int maxLength,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.NullOrEmpty(input, parameterName);

            if (input.Length > maxLength)
            {
                throw new ArgumentException(
                    $"String cannot be longer than {maxLength} characters, but was {input.Length}",
                    parameterName
                );
            }

            return input;
        }

        public static string MinLength(
            this IGuardClause guardClause,
            string input,
            int minLength,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.NullOrEmpty(input, parameterName);

            if (input.Length < minLength)
            {
                throw new ArgumentException(
                    $"String must be at least {minLength} characters, but was {input.Length}",
                    parameterName
                );
            }

            return input;
        }

        public static string OnlyDigits(
            this IGuardClause guardClause,
            string input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.NullOrWhiteSpace(input, parameterName);

            if (!input.All(char.IsDigit))
            {
                throw new ArgumentException(
                    "String must contain only digits",
                    parameterName
                );
            }

            return input;
        }

        public static string OnlyLetters(
            this IGuardClause guardClause,
            string input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.NullOrWhiteSpace(input, parameterName);

            if (!input.All(char.IsLetter))
            {
                throw new ArgumentException(
                    "String must contain only letters",
                    parameterName
                );
            }

            return input;
        }
    }
}
```

## Unit Testing Examples

```csharp
using Xunit;
using Ardalis.GuardClauses;

public class OrderTests
{
    [Fact]
    public void Constructor_WithValidInputs_CreatesOrder()
    {
        // Arrange & Act
        var order = new Order(1, "John Doe");

        // Assert
        Assert.Equal(1, order.Id);
        Assert.Equal("John Doe", order.CustomerName);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void Constructor_WithNullCustomerName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Order(1, null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespaceCustomerName_ThrowsArgumentException(
        string customerName)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new Order(1, customerName));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithNonPositiveId_ThrowsArgumentException(int id)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new Order(id, "John Doe"));
    }

    [Fact]
    public void AddItem_WithValidInputs_AddsItemToOrder()
    {
        // Arrange
        var order = new Order(1, "John Doe");

        // Act
        order.AddItem("Product 1", 2, 10.50m);

        // Assert
        Assert.Single(order.Items);
        Assert.Equal(21.00m, order.Total);
    }

    [Theory]
    [InlineData(null, 1, 10.0)]
    [InlineData("", 1, 10.0)]
    [InlineData("   ", 1, 10.0)]
    public void AddItem_WithInvalidProductName_ThrowsException(
        string productName, int quantity, decimal unitPrice)
    {
        // Arrange
        var order = new Order(1, "John Doe");

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() =>
            order.AddItem(productName, quantity, unitPrice));
    }

    [Fact]
    public void RemoveItem_WithNonExistentProduct_ThrowsNotFoundException()
    {
        // Arrange
        var order = new Order(1, "John Doe");
        order.AddItem("Product 1", 1, 10.0m);

        // Act & Assert
        Assert.Throws<NotFoundException>(() =>
            order.RemoveItem("Non-existent Product"));
    }
}

public class EmailTests
{
    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("admin+tag@company.org")]
    public void Constructor_WithValidEmail_CreatesEmail(string emailAddress)
    {
        // Arrange & Act
        var email = new Email(emailAddress);

        // Assert
        Assert.Equal(emailAddress.ToLower(), email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Constructor_WithInvalidEmail_ThrowsException(string emailAddress)
    {
        // Arrange & Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new Email(emailAddress));
    }
}
```

## Performance Considerations

```csharp
// For performance-critical code, consider caching validators
public class CachedEmailValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    private static readonly ConcurrentDictionary<string, bool> ValidationCache = new();

    public static string ValidateEmail(string email)
    {
        var normalized = Guard.Against.NullOrWhiteSpace(email).ToLower();

        var isValid = ValidationCache.GetOrAdd(normalized, key => EmailRegex.IsMatch(key));

        if (!isValid)
        {
            throw new ArgumentException("Invalid email format", nameof(email));
        }

        return normalized;
    }
}
```
