# GuardClauses API Reference

Complete API documentation for Ardalis.GuardClauses extension methods and types.

## IGuardClause Interface

The `IGuardClause` interface is the extension point for all guard clause methods.

```csharp
public interface IGuardClause
{
}
```

All guard clauses are extension methods on this interface.

## Guard Static Class

```csharp
public static class Guard
{
    public static IGuardClause Against { get; } = new GuardClause();
}
```

Usage: `Guard.Against.Null(parameter)`

## Built-in Guard Clauses

### Null Guards

#### Null<T>
```csharp
public static T Null<T>(
    this IGuardClause guardClause,
    [NotNull] T? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : class
```

**Parameters:**
- `input` - The value to check
- `parameterName` - Automatically populated with the parameter name (optional)
- `message` - Custom error message (optional)

**Returns:** The input value if not null

**Throws:** `ArgumentNullException` if input is null

**Example:**
```csharp
Guard.Against.Null(order);
Guard.Against.Null(customer, nameof(customer), "Customer is required");
```

#### Null (Value Type Overload)
```csharp
public static T Null<T>(
    this IGuardClause guardClause,
    [NotNull] T? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : struct
```

For nullable value types (e.g., `int?`, `DateTime?`).

#### NullOrEmpty (String)
```csharp
public static string NullOrEmpty(
    this IGuardClause guardClause,
    [NotNull] string? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

**Throws:**
- `ArgumentNullException` if input is null
- `ArgumentException` if input is empty

**Example:**
```csharp
string name = Guard.Against.NullOrEmpty(customerName);
```

#### NullOrEmpty (Guid)
```csharp
public static Guid NullOrEmpty(
    this IGuardClause guardClause,
    [NotNull] Guid? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

**Throws:**
- `ArgumentNullException` if input is null
- `ArgumentException` if input is Guid.Empty

#### NullOrEmpty (IEnumerable)
```csharp
public static IEnumerable<T> NullOrEmpty<T>(
    this IGuardClause guardClause,
    [NotNull] IEnumerable<T>? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

**Throws:**
- `ArgumentNullException` if input is null
- `ArgumentException` if collection is empty

**Example:**
```csharp
var items = Guard.Against.NullOrEmpty(orderItems);
```

#### NullOrWhiteSpace
```csharp
public static string NullOrWhiteSpace(
    this IGuardClause guardClause,
    [NotNull] string? input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

**Throws:**
- `ArgumentNullException` if input is null
- `ArgumentException` if input is empty or whitespace

**Example:**
```csharp
string email = Guard.Against.NullOrWhiteSpace(userEmail);
```

#### NullOrInvalidInput
```csharp
public static T NullOrInvalidInput<T>(
    this IGuardClause guardClause,
    [NotNull] T? input,
    [CallerArgumentExpression("input")] string? parameterName,
    Func<T, bool> predicate,
    string? message = null)
```

Combines null check with custom validation.

**Parameters:**
- `predicate` - Function returning true if input is valid

**Example:**
```csharp
var email = Guard.Against.NullOrInvalidInput(
    userEmail,
    nameof(userEmail),
    e => e.Contains("@"),
    "Email must contain @"
);
```

### Numeric Guards

#### Negative
```csharp
public static T Negative<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : struct, IComparable
```

**Throws:** `ArgumentException` if input < 0

**Supported types:** int, long, decimal, float, double, short, etc.

**Example:**
```csharp
decimal price = Guard.Against.Negative(unitPrice);
```

#### NegativeOrZero
```csharp
public static T NegativeOrZero<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : struct, IComparable
```

**Throws:** `ArgumentException` if input <= 0

**Example:**
```csharp
int quantity = Guard.Against.NegativeOrZero(orderQuantity);
```

#### Zero
```csharp
public static T Zero<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : struct
```

**Throws:** `ArgumentException` if input == 0

**Example:**
```csharp
long maxItems = Guard.Against.Zero(limit);
```

### Range Guards

#### OutOfRange (Generic)
```csharp
public static T OutOfRange<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName,
    T rangeFrom,
    T rangeTo,
    string? message = null)
    where T : IComparable, IComparable<T>
```

**Parameters:**
- `rangeFrom` - Minimum allowed value (inclusive)
- `rangeTo` - Maximum allowed value (inclusive)

**Throws:** `ArgumentOutOfRangeException` if input is outside range

**Example:**
```csharp
int age = Guard.Against.OutOfRange(personAge, nameof(personAge), 0, 120);
DateTime date = Guard.Against.OutOfRange(orderDate, nameof(orderDate), minDate, maxDate);
```

#### OutOfRange (Integer)
```csharp
public static int OutOfRange(
    this IGuardClause guardClause,
    int input,
    [CallerArgumentExpression("input")] string? parameterName,
    int rangeFrom,
    int rangeTo,
    string? message = null)
```

Optimized overload for integers.

#### OutOfRange (DateTime)
```csharp
public static DateTime OutOfRange(
    this IGuardClause guardClause,
    DateTime input,
    [CallerArgumentExpression("input")] string? parameterName,
    DateTime rangeFrom,
    DateTime rangeTo,
    string? message = null)
```

#### NullOrOutOfRange
```csharp
public static T? NullOrOutOfRange<T>(
    this IGuardClause guardClause,
    T? input,
    [CallerArgumentExpression("input")] string? parameterName,
    T rangeFrom,
    T rangeTo,
    string? message = null)
    where T : struct, IComparable, IComparable<T>
```

For nullable types. Allows null but validates range if not null.

**Throws:**
- `ArgumentNullException` if null (when not allowing null)
- `ArgumentOutOfRangeException` if outside range

**Example:**
```csharp
int? age = Guard.Against.NullOrOutOfRange(nullableAge, nameof(nullableAge), 0, 120);
```

#### OutOfSQLDateRange
```csharp
public static DateTime OutOfSQLDateRange(
    this IGuardClause guardClause,
    DateTime input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

Valid SQL Server DateTime range: 1753-01-01 to 9999-12-31

**Throws:** `ArgumentOutOfRangeException` if outside SQL DateTime range

**Example:**
```csharp
DateTime orderDate = Guard.Against.OutOfSQLDateRange(inputDate);
```

### Enum Guards

#### EnumOutOfRange
```csharp
public static T EnumOutOfRange<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
    where T : struct, Enum
```

**Throws:** `ArgumentOutOfRangeException` if not a defined enum value

**Example:**
```csharp
OrderStatus status = Guard.Against.EnumOutOfRange(inputStatus);
```

**Note:** In v5.0+, renamed from `OutOfRange` for enums to `EnumOutOfRange`

### String/Format Guards

#### InvalidFormat (Regex)
```csharp
public static string InvalidFormat(
    this IGuardClause guardClause,
    string input,
    [CallerArgumentExpression("input")] string? parameterName,
    string regexPattern,
    string? message = null)
```

**Parameters:**
- `regexPattern` - Regular expression pattern to match

**Throws:** `ArgumentException` if input doesn't match pattern

**Example:**
```csharp
string email = Guard.Against.InvalidFormat(
    userEmail,
    nameof(userEmail),
    @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
    "Invalid email format"
);
```

#### InvalidFormat (Predicate)
```csharp
public static string InvalidFormat(
    this IGuardClause guardClause,
    string input,
    [CallerArgumentExpression("input")] string? parameterName,
    Func<string, bool> predicate,
    string? message = null)
```

**Parameters:**
- `predicate` - Function returning true if format is valid

**Example:**
```csharp
string phone = Guard.Against.InvalidFormat(
    phoneNumber,
    nameof(phoneNumber),
    p => p.Length == 10 && p.All(char.IsDigit),
    "Phone must be 10 digits"
);
```

### Expression Guard

#### Expression
```csharp
public static T Expression<T>(
    this IGuardClause guardClause,
    Func<T, bool> predicate,
    T input,
    string message,
    [CallerArgumentExpression("input")] string? parameterName = null)
```

**BREAKING CHANGE in v5.0:** Logic reversed - now throws if predicate is TRUE.

**Parameters:**
- `predicate` - Condition that triggers exception when TRUE
- `message` - Error message to throw

**Throws:** `ArgumentException` if predicate evaluates to true

**Example:**
```csharp
// v5.0+ syntax - throws when condition is TRUE
Guard.Against.Expression(
    x => x < 0 || x > 100,
    percentage,
    "Percentage must be between 0 and 100"
);

// v4.x syntax - throws when condition is FALSE (deprecated)
Guard.Against.Expression(
    x => x >= 0 && x <= 100,
    percentage,
    "Percentage must be between 0 and 100"
);
```

### Repository Guards

#### NotFound
```csharp
public static T NotFound<T>(
    this IGuardClause guardClause,
    object key,
    [NotNull] T? input,
    [CallerArgumentExpression("input")] string? parameterName = null)
    where T : class
```

Similar to Null but throws `NotFoundException` instead of `ArgumentNullException`.

**Parameters:**
- `key` - The identifier used to search for the entity
- `input` - The entity that should have been found

**Throws:** `NotFoundException` if input is null

**Example:**
```csharp
var order = await repository.FindByIdAsync(orderId);
Guard.Against.NotFound(orderId, order);
```

#### NotFound (String Key)
```csharp
public static T NotFound<T>(
    this IGuardClause guardClause,
    string key,
    [NotNull] T? input,
    [CallerArgumentExpression("input")] string? parameterName = null)
    where T : class
```

Overload for string keys.

### Default Value Guard

#### Default
```csharp
public static T Default<T>(
    this IGuardClause guardClause,
    [AllowNull, NotNull] T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null)
```

**Throws:** `ArgumentException` if input equals default value for type

**Example:**
```csharp
Guid id = Guard.Against.Default(orderId);
int value = Guard.Against.Default(quantity);
```

## Exception Types

### NotFoundException

```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string key, string objectName)
        : base($"{objectName} with key '{key}' was not found.")
    {
    }

    public NotFoundException(string key, string objectName, Exception innerException)
        : base($"{objectName} with key '{key}' was not found.", innerException)
    {
    }
}
```

**Usage:**
```csharp
throw new NotFoundException("123", nameof(Order));
// Message: "Order with key '123' was not found."
```

## Custom Exception Creators

All guard clauses (v4.6+) support custom exception creation:

```csharp
public static T Null<T>(
    this IGuardClause guardClause,
    T? input,
    string? parameterName = null,
    string? message = null,
    Func<Exception>? exceptionCreator = null)
    where T : class
```

**Example:**
```csharp
Guard.Against.Null(
    order,
    exceptionCreator: () => new DomainException("Order cannot be null")
);

Guard.Against.Negative(
    price,
    nameof(price),
    exceptionCreator: (msg) => new ValidationException(msg)
);
```

## Attributes

### CallerArgumentExpression

Used internally to automatically capture parameter names:

```csharp
[CallerArgumentExpression("input")] string? parameterName = null
```

This C# 10 feature allows automatic parameter name capture without `nameof()`.

### NotNull

Applied to parameters to indicate they will not be null after the method returns:

```csharp
public static T Null<T>(
    this IGuardClause guardClause,
    [NotNull] T? input,
    ...)
```

### AllowNull

Allows null input but ensures non-null output:

```csharp
[AllowNull, NotNull] T input
```

## Extension Method Pattern

All guard clauses follow this pattern:

```csharp
public static TReturn GuardName<T>(
    this IGuardClause guardClause,
    T input,
    [CallerArgumentExpression("input")] string? parameterName = null,
    string? message = null,
    Func<Exception>? exceptionCreator = null)
{
    // Validation logic
    if (invalid)
    {
        if (exceptionCreator != null)
            throw exceptionCreator();
            
        if (message != null)
            throw new CustomException(message, parameterName);
            
        throw new DefaultException(parameterName);
    }
    
    return input; // Or transformed value
}
```

## Generic Constraints

Common generic constraints used:

```csharp
where T : struct                          // Value types only
where T : class                           // Reference types only
where T : struct, IComparable             // Comparable value types
where T : IComparable, IComparable<T>     // Fully comparable types
where T : struct, Enum                    // Enum types only
```

## Return Values

All guard clauses return the validated input (or transformed value), enabling direct assignment:

```csharp
_name = Guard.Against.NullOrWhiteSpace(name);
_quantity = Guard.Against.NegativeOrZero(quantity);
_email = Guard.Against.InvalidEmail(email);
```

## Null Analysis Attributes

Used for nullable reference type analysis:

| Attribute | Meaning |
|-----------|---------|
| `[NotNull]` | Parameter/return value will not be null |
| `[AllowNull]` | Parameter can be null |
| `[MaybeNull]` | Return value might be null |
| `[NotNullWhen(true)]` | Parameter not null when method returns true |

These help the compiler understand null-state after guard clauses.
