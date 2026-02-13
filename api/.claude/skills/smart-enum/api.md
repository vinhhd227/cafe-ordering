# SmartEnum API Reference

Complete API documentation for Ardalis.SmartEnum classes and methods.

## SmartEnum<TEnum, TValue> Base Class

### Properties

#### List
```csharp
public static IReadOnlyCollection<TEnum> List { get; }
```
Returns all instances of the SmartEnum.

**Example:**
```csharp
var allStatuses = OrderStatus.List;
foreach (var status in allStatuses)
{
    Console.WriteLine($"{status.Name}: {status.Value}");
}
```

#### Name
```csharp
public string Name { get; }
```
Gets the name of the SmartEnum instance.

#### Value
```csharp
public TValue Value { get; }
```
Gets the value of the SmartEnum instance.

### Static Methods

#### FromName(string name, bool ignoreCase = false)
```csharp
public static TEnum FromName(string name, bool ignoreCase = false)
```
Gets the SmartEnum instance by its name.

**Parameters:**
- `name` - The name to search for
- `ignoreCase` - Whether to ignore case during comparison (default: false)

**Returns:** The matching SmartEnum instance

**Throws:** `SmartEnumNotFoundException` if not found

**Example:**
```csharp
var status = OrderStatus.FromName("Pending");
var statusIgnoreCase = OrderStatus.FromName("pending", ignoreCase: true);
```

#### TryFromName(string name, out TEnum result)
```csharp
public static bool TryFromName(string name, out TEnum result)
```
Tries to get the SmartEnum instance by its name.

**Parameters:**
- `name` - The name to search for
- `result` - When this method returns, contains the matching instance if found; otherwise, null

**Returns:** `true` if found; otherwise, `false`

**Example:**
```csharp
if (OrderStatus.TryFromName("Pending", out var status))
{
    Console.WriteLine($"Found: {status.Name}");
}
```

#### TryFromName(string name, bool ignoreCase, out TEnum result)
```csharp
public static bool TryFromName(string name, bool ignoreCase, out TEnum result)
```
Tries to get the SmartEnum instance by its name with case-sensitivity option.

**Example:**
```csharp
if (OrderStatus.TryFromName("pending", ignoreCase: true, out var status))
{
    Console.WriteLine($"Found: {status.Name}");
}
```

#### FromValue(TValue value)
```csharp
public static TEnum FromValue(TValue value)
```
Gets the SmartEnum instance by its value.

**Parameters:**
- `value` - The value to search for

**Returns:** The first matching SmartEnum instance

**Throws:** `SmartEnumNotFoundException` if not found

**Example:**
```csharp
var status = OrderStatus.FromValue(1);
```

#### FromValue(TValue value, TEnum defaultValue)
```csharp
public static TEnum FromValue(TValue value, TEnum defaultValue)
```
Gets the SmartEnum instance by its value, or returns a default value if not found.

**Parameters:**
- `value` - The value to search for
- `defaultValue` - The value to return if not found

**Returns:** The matching instance or `defaultValue`

**Example:**
```csharp
var status = OrderStatus.FromValue(999, OrderStatus.Pending);
```

#### TryFromValue(TValue value, out TEnum result)
```csharp
public static bool TryFromValue(TValue value, out TEnum result)
```
Tries to get the SmartEnum instance by its value.

**Parameters:**
- `value` - The value to search for
- `result` - When this method returns, contains the matching instance if found; otherwise, null

**Returns:** `true` if found; otherwise, `false`

**Example:**
```csharp
if (OrderStatus.TryFromValue(1, out var status))
{
    Console.WriteLine($"Found: {status.Name}");
}
```

### Instance Methods

#### ToString()
```csharp
public override string ToString()
```
Returns the name of the SmartEnum instance.

**Example:**
```csharp
var status = OrderStatus.Pending;
Console.WriteLine(status.ToString()); // "Pending"
```

#### Equals(object obj)
```csharp
public override bool Equals(object obj)
```
Determines whether the specified object is equal to the current SmartEnum instance.

**Example:**
```csharp
var status1 = OrderStatus.Pending;
var status2 = OrderStatus.FromValue(1);
Console.WriteLine(status1.Equals(status2)); // true
```

#### GetHashCode()
```csharp
public override int GetHashCode()
```
Returns the hash code for this instance.

#### CompareTo(SmartEnum<TEnum, TValue> other)
```csharp
public virtual int CompareTo(SmartEnum<TEnum, TValue> other)
```
Compares the current instance with another SmartEnum instance based on their values.

**Returns:**
- Negative number if this instance is less than `other`
- Zero if equal
- Positive number if greater than `other`

**Example:**
```csharp
var pending = OrderStatus.Pending;
var processing = OrderStatus.Processing;
Console.WriteLine(pending.CompareTo(processing)); // -1
```

#### When(params SmartEnum<TEnum, TValue>[] smartEnums)
```csharp
public Execute<TEnum, TValue> When(params SmartEnum<TEnum, TValue>[] smartEnums)
```
Starts a conditional execution chain. Part of the When/Then pattern.

**Example:**
```csharp
var status = OrderStatus.Pending;
status.When(OrderStatus.Pending).Then(() => Console.WriteLine("Pending!"));
```

#### When(IEnumerable<SmartEnum<TEnum, TValue>> smartEnums)
```csharp
public Execute<TEnum, TValue> When(IEnumerable<SmartEnum<TEnum, TValue>> smartEnums)
```
Starts a conditional execution chain with a collection of SmartEnum instances.

### Operators

#### Equality (==)
```csharp
public static bool operator ==(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```
Determines whether two SmartEnum instances are equal.

**Example:**
```csharp
var status1 = OrderStatus.Pending;
var status2 = OrderStatus.FromValue(1);
Console.WriteLine(status1 == status2); // true
```

#### Inequality (!=)
```csharp
public static bool operator !=(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```
Determines whether two SmartEnum instances are not equal.

#### Less Than (<)
```csharp
public static bool operator <(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```
Determines whether the left instance is less than the right instance.

**Example:**
```csharp
Console.WriteLine(OrderStatus.Pending < OrderStatus.Processing); // true
```

#### Less Than or Equal (<=)
```csharp
public static bool operator <=(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```

#### Greater Than (>)
```csharp
public static bool operator >(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```

#### Greater Than or Equal (>=)
```csharp
public static bool operator >=(SmartEnum<TEnum, TValue> left, SmartEnum<TEnum, TValue> right)
```

## SmartFlagEnum<TEnum> Class

Inherits all methods from `SmartEnum<TEnum, int>` with flag-specific behavior.

### Static Methods

#### FromValue(int value)
```csharp
public static IEnumerable<TEnum> FromValue(int value)
```
Gets all SmartFlagEnum instances that match the bitwise combination.

**Returns:** Collection of matching flags

**Throws:** `SmartEnumNotFoundException` if value contains invalid flags

**Example:**
```csharp
var permissions = Permission.FromValue(3); // Returns [Read, Write]
```

#### TryFromValue(int value, out IEnumerable<TEnum> result)
```csharp
public static bool TryFromValue(int value, out IEnumerable<TEnum> result)
```
Tries to get all SmartFlagEnum instances that match the bitwise combination.

**Example:**
```csharp
if (Permission.TryFromValue(3, out var permissions))
{
    foreach (var permission in permissions)
    {
        Console.WriteLine(permission.Name);
    }
}
```

#### FromName(string name, bool ignoreCase = false)
```csharp
public static IEnumerable<TEnum> FromName(string name, bool ignoreCase = false)
```
Gets SmartFlagEnum instances by comma-separated names.

**Example:**
```csharp
var permissions = Permission.FromName("Read, Write");
var permissionsIgnoreCase = Permission.FromName("read, write", ignoreCase: true);
```

#### TryFromName(string name, out IEnumerable<TEnum> result)
```csharp
public static bool TryFromName(string name, out IEnumerable<TEnum> result)
```

#### TryFromName(string name, bool ignoreCase, out IEnumerable<TEnum> result)
```csharp
public static bool TryFromName(string name, bool ignoreCase, out IEnumerable<TEnum> result)
```

### Instance Methods

#### FromValueToString(int value)
```csharp
public static string FromValueToString(int value)
```
Converts a flag value to a comma-separated string of names.

**Example:**
```csharp
var names = Permission.FromValueToString(3); // "Read, Write"
```

### Operators

#### Bitwise OR (|)
```csharp
public static TEnum operator |(SmartFlagEnum<TEnum> left, SmartFlagEnum<TEnum> right)
```
Combines two SmartFlagEnum instances using bitwise OR.

**Example:**
```csharp
var combined = Permission.Read | Permission.Write;
Console.WriteLine(combined.Value); // 3
```

## Attributes

### SmartEnumStringComparerAttribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class SmartEnumStringComparerAttribute : Attribute
```
Specifies string comparison mode for SmartEnum name lookups.

**Constructor:**
```csharp
public SmartEnumStringComparerAttribute(StringComparison comparisonType)
```

**Example:**
```csharp
[SmartEnumStringComparer(StringComparison.OrdinalIgnoreCase)]
public sealed class Status : SmartEnum<Status>
{
    // ...
}
```

### SmartEnumNameAttribute
```csharp
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class SmartEnumNameAttribute : ValidationAttribute
```
Validates that a string value matches a valid SmartEnum name.

**Constructor:**
```csharp
public SmartEnumNameAttribute(Type enumType)
```

**Example:**
```csharp
public class CreateOrderRequest
{
    [SmartEnumName(typeof(OrderStatus))]
    public string StatusName { get; set; }
}
```

### AllowNegativeInputAttribute
```csharp
[AttributeUsage(AttributeTargets.Class)]
public class AllowNegativeInputAttribute : Attribute
```
Allows SmartFlagEnum to accept negative input values (other than -1).

**Example:**
```csharp
[AllowNegativeInput]
public sealed class Permission : SmartFlagEnum<Permission>
{
    // ...
}
```

## Exceptions

### SmartEnumNotFoundException
```csharp
public class SmartEnumNotFoundException : Exception
```
Thrown when a SmartEnum instance cannot be found by name or value.

**Properties:**
- `Message` - Error message describing what wasn't found

### SmartFlagEnumDoesNotContainPowerOfTwoValuesException
```csharp
public class SmartFlagEnumDoesNotContainPowerOfTwoValuesException : Exception
```
Thrown when SmartFlagEnum values are not powers of 2 or not consecutive.

### NegativeValueArgumentException
```csharp
public class NegativeValueArgumentException : ArgumentException
```
Thrown when a negative value (other than -1) is passed to SmartFlagEnum.FromValue() without `[AllowNegativeInput]` attribute.

## JSON Converters

### System.Text.Json

#### SmartEnumNameConverter<TEnum, TValue>
```csharp
public class SmartEnumNameConverter<TEnum, TValue> : JsonConverter<TEnum>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
```
Serializes SmartEnum as its name.

**Example:**
```csharp
[JsonConverter(typeof(SmartEnumNameConverter<OrderStatus, int>))]
public OrderStatus Status { get; set; }
```

#### SmartEnumValueConverter<TEnum, TValue>
```csharp
public class SmartEnumValueConverter<TEnum, TValue> : JsonConverter<TEnum>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
```
Serializes SmartEnum as its value.

**Example:**
```csharp
[JsonConverter(typeof(SmartEnumValueConverter<OrderStatus, int>))]
public OrderStatus Status { get; set; }
```

### Json.NET (Newtonsoft)

#### SmartEnumNameConverter<TEnum, TValue>
```csharp
public class SmartEnumNameConverter<TEnum, TValue> : JsonConverter
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
```

#### SmartEnumValueConverter<TEnum, TValue>
```csharp
public class SmartEnumValueConverter<TEnum, TValue> : JsonConverter
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
```

## EF Core Extensions

### ConfigureSmartEnum (Extension Method)
```csharp
public static void ConfigureSmartEnum(this ModelConfigurationBuilder configurationBuilder)
```
Configures all SmartEnum properties in the model to use value conversion.

**Usage in DbContext:**
```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
{
    configurationBuilder.ConfigureSmartEnum();
}
```

### ConfigureSmartEnum (Static Method)
```csharp
public static void ConfigureSmartEnum(ModelBuilder modelBuilder)
```
Configures SmartEnum value conversions for pre-EF Core 6 versions.

**Usage in DbContext:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    SmartEnumEFCoreExtensions.ConfigureSmartEnum(modelBuilder);
}
```

## Dapper Type Handlers

### SmartEnumByNameTypeHandler<TEnum>
```csharp
public class SmartEnumByNameTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
    where TEnum : SmartEnum<TEnum>
```
Dapper type handler for SmartEnum using name-based storage.

**Registration:**
```csharp
SqlMapper.AddTypeHandler(new SmartEnumByNameTypeHandler<OrderStatus>());
```

### SmartEnumByValueTypeHandler<TEnum>
```csharp
public class SmartEnumByValueTypeHandler<TEnum> : SqlMapper.TypeHandler<TEnum>
    where TEnum : SmartEnum<TEnum>
```
Dapper type handler for SmartEnum using value-based storage.

**Registration:**
```csharp
SqlMapper.AddTypeHandler(new SmartEnumByValueTypeHandler<OrderStatus>());
```

## AutoFixture Customization

### SmartEnumSpecimenBuilder
```csharp
public class SmartEnumSpecimenBuilder : ISpecimenBuilder
```
AutoFixture specimen builder for generating random SmartEnum instances.

**Registration:**
```csharp
var fixture = new Fixture();
fixture.Customizations.Add(new SmartEnumSpecimenBuilder());

// Generate random SmartEnum
var randomStatus = fixture.Create<OrderStatus>();
```

## Generic Constraints

All SmartEnum types must satisfy these constraints:

```csharp
where TEnum : SmartEnum<TEnum, TValue>
where TValue : IEquatable<TValue>, IComparable<TValue>
```

**Supported TValue types:**
- `int` (default)
- `ushort`
- `long`
- `byte`
- `short`
- `uint`
- `ulong`
- `string`
- `Guid`
- Any type implementing `IEquatable<T>` and `IComparable<T>`
