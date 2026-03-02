---
name: guard-clauses
description: Ardalis GuardClauses library for C#. Use when implementing defensive programming with guard clauses, input validation, parameter checking, or fail-fast validation. Triggers on requests involving null checks, empty string validation, range validation, numeric validation, custom guard clauses, or extension methods for IGuardClause. Also use for questions about defensive programming patterns, constructor validation, method parameter validation, or creating custom guard clause extensions.
---

# GuardClauses Skill

Implement defensive programming and input validation using Ardalis.GuardClauses library - a simple, extensible package for fail-fast parameter validation.

## What are Guard Clauses?

Guard clauses are a software pattern that simplifies complex functions by "failing fast" - checking for invalid inputs up front and immediately failing if any are found. This prevents:
- Null reference exceptions deep in call stacks
- Invalid state propagation
- Complex nested if-else validation logic
- Hard-to-debug errors

**Instead of:**
```csharp
public void ProcessOrder(Order order)
{
    if (order != null)
    {
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
        {
            if (order.Total > 0)
            {
                // Process order
            }
            else
            {
                throw new ArgumentException("Total must be positive");
            }
        }
        else
        {
            throw new ArgumentException("Customer name is required");
        }
    }
    else
    {
        throw new ArgumentNullException(nameof(order));
    }
}
```

**Use guard clauses:**
```csharp
public void ProcessOrder(Order order)
{
    Guard.Against.Null(order);
    Guard.Against.NullOrWhiteSpace(order.CustomerName);
    Guard.Against.NegativeOrZero(order.Total);
    
    // Process order - guaranteed valid inputs
}
```

## Installation

```bash
dotnet add package Ardalis.GuardClauses
```

**Latest Version**: 5.0.0 (as of September 2024)
**NuGet Downloads**: 39.8M+
**License**: MIT

## Basic Usage

### Simple Null Check

```csharp
using Ardalis.GuardClauses;

public void ProcessOrder(Order order)
{
    Guard.Against.Null(order);
    // order is guaranteed not null here
    
    // process order
}
```

### Constructor Validation with Assignment

```csharp
using Ardalis.GuardClauses;

public class Order
{
    private string _customerName;
    private int _quantity;
    private decimal _unitPrice;

    public Order(string customerName, int quantity, decimal unitPrice)
    {
        // Guard clauses return the value, enabling direct assignment
        _customerName = Guard.Against.NullOrWhiteSpace(customerName);
        _quantity = Guard.Against.NegativeOrZero(quantity);
        _unitPrice = Guard.Against.Negative(unitPrice);
    }
}
```

### With Custom Parameter Names

```csharp
// Automatic parameter name (uses CallerArgumentExpression)
Guard.Against.Null(order);
// Exception message: "Required input order cannot be null. (Parameter 'order')"

// Explicit parameter name
Guard.Against.Null(order, nameof(order));
// Same result
```

## Core Guard Clauses

### Null Validation

#### Guard.Against.Null
```csharp
public void ProcessOrder(Order? order)
{
    Guard.Against.Null(order);
    // order is now guaranteed non-null
}

// Throws: ArgumentNullException if input is null
```

#### Guard.Against.NullOrEmpty
```csharp
// For strings
string name = Guard.Against.NullOrEmpty(customerName);

// For Guid
Guid id = Guard.Against.NullOrEmpty(orderId);

// For arrays/collections
int[] items = Guard.Against.NullOrEmpty(orderItems);

// Throws: ArgumentNullException if null
// Throws: ArgumentException if empty
```

#### Guard.Against.NullOrWhiteSpace
```csharp
string name = Guard.Against.NullOrWhiteSpace(customerName);

// Throws: ArgumentNullException if null
// Throws: ArgumentException if empty or whitespace
```

### Numeric Validation

#### Guard.Against.Negative
```csharp
decimal price = Guard.Against.Negative(unitPrice);
int age = Guard.Against.Negative(personAge);

// Throws: ArgumentException if input < 0
```

#### Guard.Against.NegativeOrZero
```csharp
int quantity = Guard.Against.NegativeOrZero(orderQuantity);
decimal amount = Guard.Against.NegativeOrZero(paymentAmount);

// Throws: ArgumentException if input <= 0
```

#### Guard.Against.Zero
```csharp
long maxItems = Guard.Against.Zero(maximumLimit);

// Throws: ArgumentException if input == 0
```

#### Guard.Against.OutOfRange
```csharp
// Integer range
int age = Guard.Against.OutOfRange(personAge, nameof(personAge), 0, 120);

// DateTime range
DateTime orderDate = Guard.Against.OutOfRange(
    inputDate, 
    nameof(inputDate), 
    DateTime.Today.AddDays(-30),
    DateTime.Today
);

// Enum range
public enum Priority { Low = 1, Medium = 2, High = 3 }
var priority = Guard.Against.OutOfRange(input, nameof(input), Priority.Low, Priority.High);

// Throws: ArgumentOutOfRangeException if outside range
```

#### Guard.Against.NullOrOutOfRange
```csharp
// For nullable types
int? age = Guard.Against.NullOrOutOfRange(nullableAge, nameof(nullableAge), 0, 120);

// Throws: ArgumentNullException if null
// Throws: ArgumentOutOfRangeException if outside range
```

### Enum Validation

#### Guard.Against.EnumOutOfRange
```csharp
public enum OrderStatus
{
    Pending = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4
}

var status = Guard.Against.EnumOutOfRange(inputStatus, nameof(inputStatus));

// Throws: ArgumentOutOfRangeException if not a valid enum value
// Changed in v5.0: Now uses EnumOutOfRange instead of OutOfRange
```

### String/Format Validation

#### Guard.Against.InvalidFormat
```csharp
// With Regex pattern
string email = Guard.Against.InvalidFormat(
    userEmail, 
    nameof(userEmail), 
    @"^[^@\s]+@[^@\s]+\.[^@\s]+$"
);

// With Func predicate
string phone = Guard.Against.InvalidFormat(
    phoneNumber,
    nameof(phoneNumber),
    phone => phone.Length == 10 && phone.All(char.IsDigit),
    "Phone must be 10 digits"
);

// Throws: ArgumentException if format is invalid
```

### DateTime Validation

#### Guard.Against.OutOfSQLDateRange
```csharp
DateTime orderDate = Guard.Against.OutOfSQLDateRange(
    inputDate, 
    nameof(inputDate)
);

// Valid SQL Server DateTime range: 1753-01-01 to 9999-12-31
// Throws: ArgumentOutOfRangeException if outside SQL DateTime range
```

### Custom Expression

#### Guard.Against.Expression
```csharp
// Generic validation with custom predicate
Guard.Against.Expression(
    value => value < 0 || value > 100,
    percentage,
    "Percentage must be between 0 and 100"
);

// Note: In v5.0+, logic is: if predicate is TRUE, throw exception
// (Previously was reversed - this was a breaking change)

// Throws: ArgumentException if expression evaluates to true
```

### Repository Pattern

#### Guard.Against.NotFound
```csharp
public async Task<Order> GetOrderAsync(int orderId)
{
    var order = await _repository.FindByIdAsync(orderId);
    
    // Throws NotFoundException instead of ArgumentNullException
    Guard.Against.NotFound(orderId, order);
    
    return order;
}

// Throws: NotFoundException (custom exception) if entity is null
// Useful in repository/service layer to distinguish from parameter validation
```

## Custom Error Messages

All guard clauses support custom error messages (v4.6+):

```csharp
// Default message
Guard.Against.NegativeOrZero(quantity, nameof(quantity));
// "Required input quantity cannot be zero or negative."

// Custom message
Guard.Against.NegativeOrZero(
    quantity, 
    nameof(quantity),
    "Order quantity must be at least 1"
);
// "Order quantity must be at least 1"
```

## Custom Exceptions

You can provide custom exception factories (v4.6+):

```csharp
Guard.Against.Negative(
    price,
    nameof(price),
    exceptionCreator: () => new DomainException("Invalid price")
);

// Or with message parameter
Guard.Against.OutOfRange(
    age,
    nameof(age),
    0,
    120,
    exceptionCreator: (message) => new ValidationException(message)
);
```

## Creating Custom Guard Clauses

### Basic Custom Guard

```csharp
using System.Runtime.CompilerServices;
using Ardalis.GuardClauses;

// IMPORTANT: Use the same namespace for automatic discovery
namespace Ardalis.GuardClauses
{
    public static class CustomGuards
    {
        public static void Foo(
            this IGuardClause guardClause,
            string input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            if (input?.ToLower() == "foo")
            {
                throw new ArgumentException(
                    "Should not have been foo!", 
                    parameterName
                );
            }
        }
    }
}

// Usage
public void SomeMethod(string something)
{
    Guard.Against.Foo(something);
    // Automatically gets parameter name "something"
}
```

### Custom Guard with Return Value

```csharp
namespace Ardalis.GuardClauses
{
    public static class EmailGuard
    {
        private static readonly Regex EmailRegex = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public static string InvalidEmail(
            this IGuardClause guardClause,
            string input,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.NullOrWhiteSpace(input, parameterName);

            if (!EmailRegex.IsMatch(input))
            {
                throw new ArgumentException(
                    $"Invalid email format.", 
                    parameterName
                );
            }

            return input;
        }
    }
}

// Usage with assignment
public class User
{
    private string _email;

    public User(string email)
    {
        _email = Guard.Against.InvalidEmail(email);
    }
}
```

### Custom Guard with Complex Logic

```csharp
namespace Ardalis.GuardClauses
{
    public static class CreditCardGuard
    {
        public static string InvalidCreditCard(
            this IGuardClause guardClause,
            string input,
            [CallerArgumentExpression("input")] string? parameterName = null,
            string? customMessage = null)
        {
            var normalized = input?.Replace(" ", "").Replace("-", "");
            
            Guard.Against.NullOrWhiteSpace(normalized, parameterName);

            if (!IsValidLuhn(normalized))
            {
                throw new ArgumentException(
                    customMessage ?? "Invalid credit card number.",
                    parameterName
                );
            }

            return input;
        }

        private static bool IsValidLuhn(string cardNumber)
        {
            if (!cardNumber.All(char.IsDigit) || cardNumber.Length < 13)
                return false;

            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int digit = cardNumber[i] - '0';

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }
    }
}
```

### Custom Guard with Generic Type

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
                    "Collection cannot be empty.",
                    parameterName
                );
            }

            return input;
        }

        public static IEnumerable<T> TooManyItems<T>(
            this IGuardClause guardClause,
            IEnumerable<T> input,
            int maxItems,
            [CallerArgumentExpression("input")] string? parameterName = null)
        {
            Guard.Against.Null(input, parameterName);

            if (input.Count() > maxItems)
            {
                throw new ArgumentException(
                    $"Collection cannot contain more than {maxItems} items.",
                    parameterName
                );
            }

            return input;
        }
    }
}

// Usage
var items = Guard.Against.Empty(orderItems);
var validItems = Guard.Against.TooManyItems(products, 100);
```

## Best Practices

### 1. Always Use in Public Methods

```csharp
// ✅ Good - validate public API boundaries
public void ProcessPayment(decimal amount, string currency)
{
    Guard.Against.NegativeOrZero(amount);
    Guard.Against.NullOrWhiteSpace(currency);
    
    // Private method can assume valid inputs
    ProcessPaymentInternal(amount, currency);
}

// Private methods don't need guards (already validated)
private void ProcessPaymentInternal(decimal amount, string currency)
{
    // Implementation
}
```

### 2. Use at Constructor Entry Points

```csharp
// ✅ Good - establish invariants
public class Product
{
    private string _name;
    private decimal _price;

    public Product(string name, decimal price)
    {
        _name = Guard.Against.NullOrWhiteSpace(name);
        _price = Guard.Against.Negative(price);
        
        // Object is now in valid state
    }
}
```

### 3. Leverage Return Values

```csharp
// ✅ Good - validate and assign in one line
_email = Guard.Against.InvalidEmail(email);

// ❌ Bad - extra code, not using return value
Guard.Against.NullOrWhiteSpace(email);
_email = email;
```

### 4. Use CallerArgumentExpression

```csharp
// ✅ Good - automatic parameter name
public static void MyGuard(
    this IGuardClause guardClause,
    string input,
    [CallerArgumentExpression("input")] string? parameterName = null)
{
    // parameterName automatically populated
}

// ❌ Bad - manual parameter name required
public static void MyGuard(
    this IGuardClause guardClause,
    string input,
    string parameterName)
{
    // User must always provide parameter name
}
```

### 5. Use Same Namespace for Extensions

```csharp
// ✅ Good - automatic discovery
namespace Ardalis.GuardClauses
{
    public static class MyGuards { }
}

// ❌ Bad - requires additional using statement
namespace MyApp.Guards
{
    public static class MyGuards { }
}
```

### 6. Provide Meaningful Error Messages

```csharp
// ✅ Good - clear, actionable message
Guard.Against.OutOfRange(
    age, 
    nameof(age), 
    0, 
    120,
    "Age must be between 0 and 120 years"
);

// ❌ Bad - generic message
Guard.Against.OutOfRange(age, nameof(age), 0, 120);
```

### 7. Combine Multiple Guards

```csharp
// ✅ Good - validate all parameters
public void CreateOrder(
    string customerName, 
    int quantity, 
    decimal price)
{
    Guard.Against.NullOrWhiteSpace(customerName);
    Guard.Against.NegativeOrZero(quantity);
    Guard.Against.Negative(price);
}
```

## Common Patterns

### Domain Entity Constructor

```csharp
public class Customer
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public DateTime CreatedDate { get; private set; }

    public Customer(
        int id, 
        string name, 
        string email,
        DateTime createdDate)
    {
        Id = Guard.Against.NegativeOrZero(id);
        Name = Guard.Against.NullOrWhiteSpace(name);
        Email = Guard.Against.InvalidEmail(email);
        CreatedDate = Guard.Against.OutOfSQLDateRange(createdDate);
    }
}
```

### Service Method

```csharp
public class OrderService
{
    public async Task<Order> ProcessOrderAsync(
        int customerId,
        List<OrderItem> items)
    {
        Guard.Against.NegativeOrZero(customerId);
        Guard.Against.NullOrEmpty(items);

        var customer = await _customerRepository.GetByIdAsync(customerId);
        Guard.Against.NotFound(customerId, customer);

        // Process order
        return new Order(customer, items);
    }
}
```

### Repository Pattern

```csharp
public class Repository<T> where T : class
{
    public async Task<T> GetByIdAsync(int id)
    {
        Guard.Against.NegativeOrZero(id);

        var entity = await _context.Set<T>().FindAsync(id);
        Guard.Against.NotFound(id, entity);

        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        Guard.Against.Null(entity);
        
        _context.Update(entity);
        await _context.SaveChangesAsync();
    }
}
```

### Value Object Pattern

```csharp
public class Email
{
    public string Value { get; }

    public Email(string value)
    {
        Value = Guard.Against.InvalidEmail(value);
    }

    public static implicit operator string(Email email) => email.Value;
    public static explicit operator Email(string value) => new(value);
}
```

### Command/Query Pattern

```csharp
public class CreateOrderCommand
{
    public int CustomerId { get; }
    public List<OrderItem> Items { get; }

    public CreateOrderCommand(int customerId, List<OrderItem> items)
    {
        CustomerId = Guard.Against.NegativeOrZero(customerId);
        Items = Guard.Against.NullOrEmpty(items);
    }
}

public class CreateOrderCommandHandler
{
    public async Task<Order> HandleAsync(CreateOrderCommand command)
    {
        Guard.Against.Null(command);
        
        // Command properties already validated in constructor
        // No need to re-validate
        
        return await CreateOrderAsync(command);
    }
}
```

## Breaking Changes

### Version 5.0 Changes

**Guard.Against.Expression Logic Reversal:**
```csharp
// v4.x and earlier - throws when expression is FALSE
Guard.Against.Expression(x => x > 0, value, "Must be positive");

// v5.0+ - throws when expression is TRUE
Guard.Against.Expression(x => x <= 0, value, "Must be positive");
```

**EnumOutOfRange:**
```csharp
// v4.x - used OutOfRange
Guard.Against.OutOfRange(status, nameof(status));

// v5.0+ - use EnumOutOfRange
Guard.Against.EnumOutOfRange(status, nameof(status));
```

## Exceptions Thrown

| Guard Clause | Exception Type |
|--------------|----------------|
| Null | ArgumentNullException |
| NullOrEmpty | ArgumentNullException or ArgumentException |
| NullOrWhiteSpace | ArgumentNullException or ArgumentException |
| Negative | ArgumentException |
| NegativeOrZero | ArgumentException |
| Zero | ArgumentException |
| OutOfRange | ArgumentOutOfRangeException |
| EnumOutOfRange | ArgumentOutOfRangeException |
| OutOfSQLDateRange | ArgumentOutOfRangeException |
| InvalidFormat | ArgumentException |
| Expression | ArgumentException |
| NotFound | NotFoundException |

## Integration with Code Analysis

To prevent CA1062 warnings when using GuardClauses, add to `.editorconfig`:

```ini
[*.cs]
dotnet_code_quality.null_check_validation_methods = Ardalis.GuardClauses.GuardClauseExtensions.Null(Ardalis.GuardClauses.IGuardClause,System.Object,System.String)|Ardalis.GuardClauses.GuardClauseExtensions.NullOrEmpty(Ardalis.GuardClauses.IGuardClause,System.String,System.String)|Ardalis.GuardClauses.GuardClauseExtensions.NullOrWhiteSpace(Ardalis.GuardClauses.IGuardClause,System.String,System.String)
```

## Testing with Guard Clauses

```csharp
using Xunit;
using Ardalis.GuardClauses;

public class OrderTests
{
    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new Order(null!, 5, 10.0m)
        );
    }

    [Fact]
    public void Constructor_WithNegativeQuantity_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new Order("Product", -1, 10.0m)
        );
        
        Assert.Contains("quantity", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithZeroOrNegativeQuantity_ThrowsException(int quantity)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new Order("Product", quantity, 10.0m)
        );
    }
}
```

## Troubleshooting

### Issue: CA1062 Warnings

**Problem**: Code analysis shows CA1062 warnings even with guards.

**Solution**: Add guards to `.editorconfig` null check validation methods (see Integration section above).

### Issue: Parameter Name Not Showing in Exception

**Problem**: Exception message shows wrong parameter name or no parameter name.

**Solution**: Ensure you're using `[CallerArgumentExpression]` attribute:
```csharp
public static void MyGuard(
    this IGuardClause guardClause,
    string input,
    [CallerArgumentExpression("input")] string? parameterName = null)
```

### Issue: Custom Guard Not Discovered

**Problem**: Custom guard extension not available.

**Solution**: Use namespace `Ardalis.GuardClauses`:
```csharp
namespace Ardalis.GuardClauses
{
    public static class MyGuards { }
}
```

## When to Use Guard Clauses

### ✅ Use Guard Clauses When:
- Validating method parameters
- Enforcing constructor invariants
- Checking preconditions in public APIs
- Implementing fail-fast validation
- Building domain entities with invariants
- Creating value objects
- Repository/service layer validation

### ❌ Don't Use Guard Clauses When:
- Validating user input in UI layer (use proper validation framework)
- Complex business rule validation (use specification pattern)
- Workflow validation (use state machines)
- Cross-cutting validation (use FluentValidation or similar)
- Performance-critical tight loops (overhead may matter)

## Workflow

1. **Identify validation needs**: Determine which parameters need validation
2. **Choose appropriate guards**: Select built-in guards or create custom ones
3. **Place at entry points**: Add guards at method/constructor entry
4. **Leverage return values**: Use guard return values for assignment
5. **Create custom extensions**: Build reusable guards for domain logic
6. **Test exception paths**: Verify guards throw correct exceptions

## References

- GitHub Repository: https://github.com/ardalis/GuardClauses
- NuGet Package: https://www.nuget.org/packages/Ardalis.GuardClauses
- Latest Version: 5.0.0
- License: MIT
- Blog Post: https://ardalis.com/guard-clauses-and-exceptions-or-validation/
