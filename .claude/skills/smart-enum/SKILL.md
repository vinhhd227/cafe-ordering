---
name: smart-enum
description: Ardalis SmartEnum library for C#. Use when creating type-safe, object-oriented alternatives to traditional C# enums with additional behavior and functionality. Triggers on requests involving SmartEnum implementation, enum classes, strongly-typed enumerations, SmartFlagEnum for bit flags, EF Core integration, JSON serialization (System.Text.Json, Json.NET), and adding behavior/methods to enum values. Also use for questions about enum replacement patterns, value object enumerations, or when users want to associate data/behavior with enum-like constants.
---

# SmartEnum Skill

Build type-safe, object-oriented alternatives to C# traditional enums using Ardalis.SmartEnum library.

## What is SmartEnum?

SmartEnum is a base class library that allows you to create strongly-typed enum replacements in C#. Unlike traditional enums which are just named integer constants, SmartEnum classes can:
- Have associated behavior (methods)
- Store additional data beyond just name and value
- Provide type safety at compile time
- Support custom value types (not just integers)
- Integrate seamlessly with EF Core, JSON serializers, and other frameworks

## Installation

```bash
# Core library
dotnet add package Ardalis.SmartEnum

# Optional packages for specific integrations
dotnet add package Ardalis.SmartEnum.EFCore          # Entity Framework Core
dotnet add package Ardalis.SmartEnum.SystemTextJson  # System.Text.Json
dotnet add package Ardalis.SmartEnum.JsonNet         # Newtonsoft Json.NET
dotnet add package Ardalis.SmartEnum.Dapper          # Dapper ORM
dotnet add package Ardalis.SmartEnum.AutoFixture     # AutoFixture testing
dotnet add package Ardalis.SmartEnum.MessagePack     # MessagePack serialization
dotnet add package Ardalis.SmartEnum.ProtoBufNet     # Protocol Buffers
```

**Latest Version**: 8.2.0+ (as of November 2024)

## Basic Usage

### Simple SmartEnum

```csharp
using Ardalis.SmartEnum;

public sealed class EmployeeType : SmartEnum<EmployeeType>
{
    public static readonly EmployeeType Manager = new(nameof(Manager), 1);
    public static readonly EmployeeType Assistant = new(nameof(Assistant), 2);
    public static readonly EmployeeType Director = new(nameof(Director), 3);

    private EmployeeType(string name, int value) : base(name, value)
    {
    }
}
```

### SmartEnum with Custom Value Type

```csharp
using Ardalis.SmartEnum;

public sealed class TestEnum : SmartEnum<TestEnum, ushort>
{
    public static readonly TestEnum One = new("First", 1);
    public static readonly TestEnum Two = new("Second", 2);
    public static readonly TestEnum Three = new("Third", 3);

    private TestEnum(string name, ushort value) : base(name, value)
    {
    }
}
```

**Supported Value Types**: Any type that implements `IEquatable<T>` and `IComparable<T>` (e.g., `int`, `ushort`, `long`, `string`, `Guid`)

### SmartEnum with Additional Properties

```csharp
using Ardalis.SmartEnum;

public sealed class Priority : SmartEnum<Priority>
{
    public static readonly Priority Low = new(nameof(Low), 1, "#00FF00");
    public static readonly Priority Medium = new(nameof(Medium), 2, "#FFFF00");
    public static readonly Priority High = new(nameof(High), 3, "#FF0000");

    public string HexColor { get; }

    private Priority(string name, int value, string hexColor) : base(name, value)
    {
        HexColor = hexColor;
    }
}

// Usage
var priority = Priority.High;
Console.WriteLine(priority.HexColor); // Output: #FF0000
```

### SmartEnum with Behavior (Abstract Pattern)

```csharp
using Ardalis.SmartEnum;

public abstract class EmployeeType : SmartEnum<EmployeeType>
{
    public static readonly EmployeeType Manager = new ManagerType();
    public static readonly EmployeeType Assistant = new AssistantType();
    public static readonly EmployeeType Director = new DirectorType();

    private EmployeeType(string name, int value) : base(name, value)
    {
    }

    public abstract decimal BonusSize { get; }
    public abstract void ProcessBonus();

    private sealed class ManagerType : EmployeeType
    {
        public ManagerType() : base("Manager", 1) { }
        public override decimal BonusSize => 10_000m;
        public override void ProcessBonus() 
        {
            Console.WriteLine($"Processing manager bonus: {BonusSize:C}");
        }
    }

    private sealed class AssistantType : EmployeeType
    {
        public AssistantType() : base("Assistant", 2) { }
        public override decimal BonusSize => 1_000m;
        public override void ProcessBonus() 
        {
            Console.WriteLine($"Processing assistant bonus: {BonusSize:C}");
        }
    }

    private sealed class DirectorType : EmployeeType
    {
        public DirectorType() : base("Director", 3) { }
        public override decimal BonusSize => 100_000m;
        public override void ProcessBonus() 
        {
            Console.WriteLine($"Processing director bonus: {BonusSize:C}");
        }
    }
}

// Usage
var employee = EmployeeType.Director;
employee.ProcessBonus(); // Output: Processing director bonus: $100,000.00
Console.WriteLine(employee.BonusSize); // Output: 100000
```

## Core Methods and Properties

### Static Properties

```csharp
// Get all enum instances
IReadOnlyCollection<EmployeeType> all = EmployeeType.List;

// Iterate through all values
foreach (var type in EmployeeType.List)
{
    Console.WriteLine($"{type.Name}: {type.Value}");
}
```

### FromName() - Get by Name

```csharp
// Get enum by name (case-sensitive)
var manager = EmployeeType.FromName("Manager");

// Get enum by name (case-insensitive)
var assistant = EmployeeType.FromName("assistant", ignoreCase: true);

// Throws SmartEnumNotFoundException if not found
try
{
    var invalid = EmployeeType.FromName("CEO");
}
catch (SmartEnumNotFoundException ex)
{
    Console.WriteLine($"Not found: {ex.Message}");
}
```

### TryFromName() - Safe Name Lookup

```csharp
// Try to get enum by name (returns bool)
if (EmployeeType.TryFromName("Manager", out var manager))
{
    Console.WriteLine($"Found: {manager.Name}");
}

// With case-insensitive search
if (EmployeeType.TryFromName("director", ignoreCase: true, out var director))
{
    Console.WriteLine($"Found: {director.Name}");
}
```

### FromValue() - Get by Value

```csharp
// Get enum by value
var type = EmployeeType.FromValue(1); // Returns Manager

// Throws SmartEnumNotFoundException if not found
try
{
    var invalid = EmployeeType.FromValue(999);
}
catch (SmartEnumNotFoundException ex)
{
    Console.WriteLine($"Value not found: {ex.Message}");
}
```

### FromValue() with Default

```csharp
// Get enum by value or return default if not found
var typeOrDefault = EmployeeType.FromValue(999, EmployeeType.Manager);
// Returns Manager if 999 doesn't exist
```

### TryFromValue() - Safe Value Lookup

```csharp
// Try to get enum by value (returns bool)
if (EmployeeType.TryFromValue(2, out var assistant))
{
    Console.WriteLine($"Found: {assistant.Name}");
}
```

### ToString() Override

```csharp
var manager = EmployeeType.Manager;
Console.WriteLine(manager.ToString()); // Output: "Manager"
Console.WriteLine(manager.Name);       // Output: "Manager"
Console.WriteLine(manager.Value);      // Output: 1
```

### Equality and Comparison

```csharp
var manager1 = EmployeeType.FromName("Manager");
var manager2 = EmployeeType.FromValue(1);

// Equality
Console.WriteLine(manager1 == manager2);           // true
Console.WriteLine(manager1.Equals(manager2));      // true

// Comparison (based on Value)
Console.WriteLine(EmployeeType.Manager < EmployeeType.Director);  // true
Console.WriteLine(EmployeeType.Manager.CompareTo(EmployeeType.Assistant)); // -1
```

## SmartFlagEnum - Bit Flag Support

SmartFlagEnum provides flag-style enum functionality similar to the `[Flags]` attribute on traditional enums.

### Basic SmartFlagEnum

```csharp
using Ardalis.SmartEnum;

public sealed class PaymentMethod : SmartFlagEnum<PaymentMethod>
{
    public static readonly PaymentMethod None = new(nameof(None), 0);
    public static readonly PaymentMethod Card = new(nameof(Card), 1);
    public static readonly PaymentMethod Cash = new(nameof(Cash), 2);
    public static readonly PaymentMethod BankTransfer = new(nameof(BankTransfer), 4);
    public static readonly PaymentMethod Crypto = new(nameof(Crypto), 8);

    private PaymentMethod(string name, int value) : base(name, value)
    {
    }
}
```

**IMPORTANT**: Flag values MUST be powers of 2 (0, 1, 2, 4, 8, 16, 32, etc.). If values are not powers of 2 or are not consecutive, a `SmartFlagEnumDoesNotContainPowerOfTwoValuesException` will be thrown.

### SmartFlagEnum with Custom Behavior

```csharp
public abstract class PaymentMethod : SmartFlagEnum<PaymentMethod>
{
    public static readonly PaymentMethod None = new NoneType();
    public static readonly PaymentMethod Card = new CardType();
    public static readonly PaymentMethod Cash = new CashType();
    public static readonly PaymentMethod BankTransfer = new BankTransferType();

    private PaymentMethod(string name, int value) : base(name, value)
    {
    }

    public abstract decimal TransactionFee { get; }

    private sealed class NoneType : PaymentMethod
    {
        public NoneType() : base("None", 0) { }
        public override decimal TransactionFee => 0m;
    }

    private sealed class CardType : PaymentMethod
    {
        public CardType() : base("Card", 1) { }
        public override decimal TransactionFee => 0.029m; // 2.9%
    }

    private sealed class CashType : PaymentMethod
    {
        public CashType() : base("Cash", 2) { }
        public override decimal TransactionFee => 0m;
    }

    private sealed class BankTransferType : PaymentMethod
    {
        public BankTransferType() : base("BankTransfer", 4) { }
        public override decimal TransactionFee => 0.01m; // 1%
    }
}
```

### SmartFlagEnum Usage

```csharp
// FromValue returns IEnumerable<SmartFlagEnum>
var methods = PaymentMethod.FromValue(3).ToList(); // Returns [Card, Cash]
var singleMethod = PaymentMethod.FromValue(1).First(); // Returns Card

// Bitwise OR operator
var combined = PaymentMethod.Card | PaymentMethod.Cash;
Console.WriteLine(combined.Value); // Output: 3

// Check if flag is set
var acceptedMethods = PaymentMethod.FromValue(5); // Card + BankTransfer
var hasCard = acceptedMethods.Contains(PaymentMethod.Card); // true
var hasCash = acceptedMethods.Contains(PaymentMethod.Cash); // false

// Get all fees
var totalFee = PaymentMethod.FromValue(3)
    .Sum(p => p.TransactionFee); // 0.029
```

### Explicit Flag Values

You can provide explicit values above the highest power of 2:

```csharp
public sealed class PaymentMethod : SmartFlagEnum<PaymentMethod>
{
    public static readonly PaymentMethod None = new(nameof(None), 0);
    public static readonly PaymentMethod Card = new(nameof(Card), 1);
    public static readonly PaymentMethod Cash = new(nameof(Cash), 2);
    public static readonly PaymentMethod All = new(nameof(All), 3);  // Explicit combination

    private PaymentMethod(string name, int value) : base(name, value)
    {
    }
}

// FromValue(3) will return the "All" instance instead of [Card, Cash]
var all = PaymentMethod.FromValue(3).First(); // Returns All
```

### Allow Negative Input

By default, negative values (except -1) throw `NegativeValueArgumentException`. Use `[AllowNegativeInput]` to disable this:

```csharp
[AllowNegativeInput]
public sealed class PaymentMethod : SmartFlagEnum<PaymentMethod>
{
    public static readonly PaymentMethod None = new(nameof(None), 0);
    public static readonly PaymentMethod Card = new(nameof(Card), 1);
    public static readonly PaymentMethod All = new(nameof(All), -1);

    private PaymentMethod(string name, int value) : base(name, value)
    {
    }
}
```

### FromValue() and TryFromValue() for Flags

```csharp
// FromValue - throws exception if not found
var methods = PaymentMethod.FromValue(3); // Returns IEnumerable

// TryFromValue - returns false if not found
if (PaymentMethod.TryFromValue(3, out var result))
{
    foreach (var method in result)
    {
        Console.WriteLine(method.Name);
    }
}
```

## When/Then Pattern (Switch-like)

SmartEnum supports fluent pattern matching:

```csharp
var employee = EmployeeType.Manager;

employee
    .When(EmployeeType.Manager).Then(() => Console.WriteLine("Manager bonus"))
    .When(EmployeeType.Director).Then(() => Console.WriteLine("Director bonus"))
    .When(EmployeeType.Assistant).Then(() => Console.WriteLine("Assistant bonus"));

// With multiple conditions
employee
    .When(EmployeeType.Manager, EmployeeType.Director)
    .Then(() => Console.WriteLine("Senior staff bonus"));
```

## Entity Framework Core Integration

### Installation

```bash
dotnet add package Ardalis.SmartEnum.EFCore
```

### Manual Value Conversion (EF Core 2.1+)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class Policy
{
    public int Id { get; set; }
    public string Name { get; set; }
    public PolicyStatus Status { get; set; }
}

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.Property(p => p.Status)
            .HasConversion(
                p => p.Value,                    // To database
                p => PolicyStatus.FromValue(p)   // From database
            );
    }
}
```

**Note**: Before EF Core 6.x, you needed a parameterless constructor. This is no longer required in EF Core 6+.

### Pre-Convention Configuration (EF Core 6+)

```csharp
using Ardalis.SmartEnum.EFCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : DbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Automatically configures all SmartEnum properties
        configurationBuilder.ConfigureSmartEnum();
    }
}
```

### Manual OnModelCreating (Pre-EF Core 6)

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    SmartEnumEFCoreExtensions.ConfigureSmartEnum(modelBuilder);
}
```

### Querying with SmartEnum in EF Core

```csharp
// Query by SmartEnum value
var policies = await context.Policies
    .Where(p => p.Status == PolicyStatus.Active)
    .ToListAsync();

// Query by SmartEnum value property
var highPriorities = await context.Tasks
    .Where(t => t.Priority.Value > 5)
    .ToListAsync();
```

## JSON Serialization

### System.Text.Json

```bash
dotnet add package Ardalis.SmartEnum.SystemTextJson
```

#### By Name

```csharp
using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

public class Employee
{
    [JsonConverter(typeof(SmartEnumNameConverter<EmployeeType, int>))]
    public EmployeeType Type { get; set; }
}

// Serializes as: { "Type": "Manager" }
```

#### By Value

```csharp
using System.Text.Json.Serialization;
using Ardalis.SmartEnum.SystemTextJson;

public class Employee
{
    [JsonConverter(typeof(SmartEnumValueConverter<EmployeeType, int>))]
    public EmployeeType Type { get; set; }
}

// Serializes as: { "Type": 1 }
```

### Json.NET (Newtonsoft)

```bash
dotnet add package Ardalis.SmartEnum.JsonNet
```

#### By Name

```csharp
using Newtonsoft.Json;
using Ardalis.SmartEnum.JsonNet;

public class Employee
{
    [JsonConverter(typeof(SmartEnumNameConverter<EmployeeType, int>))]
    public EmployeeType Type { get; set; }
}
```

#### By Value

```csharp
using Newtonsoft.Json;
using Ardalis.SmartEnum.JsonNet;

public class Employee
{
    [JsonConverter(typeof(SmartEnumValueConverter<EmployeeType, int>))]
    public EmployeeType Type { get; set; }
}
```

## Case-Insensitive String Enum

Use the `SmartEnumStringComparerAttribute` for case-insensitive comparisons:

```csharp
using Ardalis.SmartEnum;

[SmartEnumStringComparer(StringComparison.OrdinalIgnoreCase)]
public sealed class Status : SmartEnum<Status>
{
    public static readonly Status Active = new(nameof(Active), 1);
    public static readonly Status Inactive = new(nameof(Inactive), 2);

    private Status(string name, int value) : base(name, value)
    {
    }
}

// Case-insensitive lookup
var active = Status.FromName("ACTIVE");  // Works
var inactive = Status.FromName("inactive");  // Works
```

## Name Validation Attribute

Validate SmartEnum names in model binding:

```csharp
using Ardalis.SmartEnum;
using System.ComponentModel.DataAnnotations;

public class CreateEmployeeRequest
{
    [SmartEnumName(typeof(EmployeeType))]
    public string EmployeeTypeName { get; set; }
}

// Valid: "Manager", "Assistant", "Director"
// Invalid: "CEO" (throws validation error)
```

## Dapper Integration

```bash
dotnet add package Ardalis.SmartEnum.Dapper
```

```csharp
using Ardalis.SmartEnum.Dapper;
using Dapper;

// Register type handlers (call once at startup)
SqlMapper.AddTypeHandler(new SmartEnumByNameTypeHandler<EmployeeType>());
// or
SqlMapper.AddTypeHandler(new SmartEnumByValueTypeHandler<EmployeeType>());

// Query with Dapper
var employees = connection.Query<Employee>("SELECT * FROM Employees");
```

## AutoFixture Support

```bash
dotnet add package Ardalis.SmartEnum.AutoFixture
```

```csharp
using AutoFixture;
using Ardalis.SmartEnum.AutoFixture;

var fixture = new Fixture();
fixture.Customizations.Add(new SmartEnumSpecimenBuilder());

// Generate random SmartEnum values in tests
var randomEmployeeType = fixture.Create<EmployeeType>();
```

## Best Practices

### 1. Always Use `sealed` or `abstract`

```csharp
// ✅ Good - sealed prevents inheritance
public sealed class Priority : SmartEnum<Priority>
{
    // ...
}

// ✅ Good - abstract for behavior pattern
public abstract class EmployeeType : SmartEnum<EmployeeType>
{
    // ...
}

// ❌ Bad - allows unintended inheritance
public class Status : SmartEnum<Status>
{
    // ...
}
```

### 2. Use Private Constructors

```csharp
// ✅ Good - prevents external instantiation
private Priority(string name, int value) : base(name, value)
{
}

// ❌ Bad - allows external creation
public Priority(string name, int value) : base(name, value)
{
}
```

### 3. Use `nameof()` for Names

```csharp
// ✅ Good - refactoring-safe
public static readonly Priority High = new(nameof(High), 3);

// ❌ Bad - string literals can get out of sync
public static readonly Priority High = new("High", 3);
```

### 4. Group Related Behavior

```csharp
// ✅ Good - behavior encapsulated in SmartEnum
public abstract class ShippingMethod : SmartEnum<ShippingMethod>
{
    public abstract decimal CalculateCost(decimal weight);
    public abstract int EstimatedDays { get; }
}

// ❌ Bad - switch statements scattered in code
decimal CalculateShippingCost(ShippingMethodEnum method, decimal weight)
{
    switch (method)
    {
        case ShippingMethodEnum.Express:
            return weight * 10;
        // ...
    }
}
```

### 5. Use Try* Methods for User Input

```csharp
// ✅ Good - handles invalid input gracefully
if (Priority.TryFromName(userInput, ignoreCase: true, out var priority))
{
    // Use priority
}

// ❌ Bad - throws exception for invalid input
try
{
    var priority = Priority.FromName(userInput);
}
catch (SmartEnumNotFoundException)
{
    // Handle error
}
```

### 6. Consider Custom Value Types

```csharp
// ✅ Good - using Guid as value type for globally unique identifiers
public sealed class TenantId : SmartEnum<TenantId, Guid>
{
    public static readonly TenantId Acme = new(nameof(Acme), Guid.Parse("..."));
    
    private TenantId(string name, Guid value) : base(name, value)
    {
    }
}
```

## Common Patterns

### Domain Status Pattern

```csharp
public abstract class OrderStatus : SmartEnum<OrderStatus>
{
    public static readonly OrderStatus Pending = new PendingStatus();
    public static readonly OrderStatus Processing = new ProcessingStatus();
    public static readonly OrderStatus Shipped = new ShippedStatus();
    public static readonly OrderStatus Delivered = new DeliveredStatus();

    private OrderStatus(string name, int value) : base(name, value)
    {
    }

    public abstract bool CanCancel { get; }
    public abstract OrderStatus NextStatus { get; }

    private sealed class PendingStatus : OrderStatus
    {
        public PendingStatus() : base("Pending", 1) { }
        public override bool CanCancel => true;
        public override OrderStatus NextStatus => Processing;
    }

    private sealed class ProcessingStatus : OrderStatus
    {
        public ProcessingStatus() : base("Processing", 2) { }
        public override bool CanCancel => true;
        public override OrderStatus NextStatus => Shipped;
    }

    private sealed class ShippedStatus : OrderStatus
    {
        public ShippedStatus() : base("Shipped", 3) { }
        public override bool CanCancel => false;
        public override OrderStatus NextStatus => Delivered;
    }

    private sealed class DeliveredStatus : OrderStatus
    {
        public DeliveredStatus() : base("Delivered", 4) { }
        public override bool CanCancel => false;
        public override OrderStatus NextStatus => this;
    }
}
```

### Strategy Pattern

```csharp
public abstract class DiscountType : SmartEnum<DiscountType>
{
    public static readonly DiscountType None = new NoneType();
    public static readonly DiscountType Percentage = new PercentageType();
    public static readonly DiscountType FixedAmount = new FixedAmountType();

    private DiscountType(string name, int value) : base(name, value)
    {
    }

    public abstract decimal Apply(decimal originalPrice, decimal discountValue);

    private sealed class NoneType : DiscountType
    {
        public NoneType() : base("None", 0) { }
        public override decimal Apply(decimal originalPrice, decimal discountValue) 
            => originalPrice;
    }

    private sealed class PercentageType : DiscountType
    {
        public PercentageType() : base("Percentage", 1) { }
        public override decimal Apply(decimal originalPrice, decimal discountValue) 
            => originalPrice * (1 - discountValue / 100);
    }

    private sealed class FixedAmountType : DiscountType
    {
        public FixedAmountType() : base("FixedAmount", 2) { }
        public override decimal Apply(decimal originalPrice, decimal discountValue) 
            => Math.Max(0, originalPrice - discountValue);
    }
}

// Usage
var discount = DiscountType.Percentage;
var finalPrice = discount.Apply(100m, 20m); // $80
```

## Troubleshooting

### Exception: SmartEnumNotFoundException

**Cause**: Trying to access a SmartEnum by name or value that doesn't exist.

**Solution**: Use `TryFromName()` or `TryFromValue()` for safe lookups:

```csharp
if (EmployeeType.TryFromValue(5, out var type))
{
    // Use type
}
else
{
    // Handle not found
}
```

### Exception: SmartFlagEnumDoesNotContainPowerOfTwoValuesException

**Cause**: SmartFlagEnum values are not powers of 2 or are not consecutive.

**Solution**: Ensure all flag values are powers of 2 (1, 2, 4, 8, 16...):

```csharp
// ✅ Correct
public static readonly PaymentMethod Card = new(nameof(Card), 1);
public static readonly PaymentMethod Cash = new(nameof(Cash), 2);
public static readonly PaymentMethod Bank = new(nameof(Bank), 4);

// ❌ Wrong - 3 is not a power of 2
public static readonly PaymentMethod Cash = new(nameof(Cash), 3);
```

### Exception: NegativeValueArgumentException

**Cause**: Passing negative value (other than -1) to SmartFlagEnum.FromValue().

**Solution**: Use `[AllowNegativeInput]` attribute if you need negative values:

```csharp
[AllowNegativeInput]
public sealed class PaymentMethod : SmartFlagEnum<PaymentMethod>
{
    // ...
}
```

### EF Core: MissingMethodException

**Cause**: EF Core version mismatch (common in EF Core 6 upgrade).

**Solution**: Update to latest Ardalis.SmartEnum.EFCore package that supports your EF Core version.

### JSON Serialization Issues

**Cause**: Missing or incorrect JsonConverter attribute.

**Solution**: Add the appropriate converter:

```csharp
// For System.Text.Json
[JsonConverter(typeof(SmartEnumNameConverter<EmployeeType, int>))]
public EmployeeType Type { get; set; }

// For Json.NET
[JsonConverter(typeof(SmartEnumNameConverter<EmployeeType, int>))]
public EmployeeType Type { get; set; }
```

## Workflow

1. **Identify enum candidates**: Look for enums with associated behavior or data
2. **Choose pattern**: Simple sealed class vs. abstract class with behavior
3. **Define SmartEnum**: Create class inheriting from `SmartEnum<T>` or `SmartEnum<T, TValue>`
4. **Add properties/methods**: Include any behavior or data specific to each value
5. **Configure serialization**: Add JSON converters if needed
6. **Configure EF Core**: Add value conversions or use `ConfigureSmartEnum()`
7. **Replace usage**: Replace traditional enum with SmartEnum throughout codebase

## When to Use SmartEnum vs Traditional Enum

### Use SmartEnum When:
- You need to associate behavior with enum values
- You need to store additional data beyond name/value
- You want compile-time type safety with rich functionality
- You need to eliminate switch statements
- You want better domain modeling
- You need custom value types (Guid, string, etc.)

### Use Traditional Enum When:
- Simple named constants with no associated behavior
- Performance is critical (SmartEnum has slight overhead)
- Interop with legacy code or external APIs expecting enums
- Framework constraints (some serializers only support enums)

## References

- GitHub Repository: https://github.com/ardalis/SmartEnum
- NuGet Package: https://www.nuget.org/packages/Ardalis.SmartEnum/
- Latest Version: 8.2.0
- License: MIT

## Additional Packages

- **SmartEnum.EFCore**: Entity Framework Core integration
- **SmartEnum.SystemTextJson**: System.Text.Json serialization
- **SmartEnum.JsonNet**: Newtonsoft Json.NET serialization
- **SmartEnum.Dapper**: Dapper ORM support
- **SmartEnum.AutoFixture**: AutoFixture test data generation
- **SmartEnum.MessagePack**: MessagePack serialization
- **SmartEnum.ProtoBufNet**: Protocol Buffers serialization
- **SmartEnum.Utf8Json**: Utf8Json serialization
