---
name: specification
description: Ardalis Specification pattern library for .NET. Use when implementing query specifications, repository pattern with specifications, Domain-Driven Design (DDD) query logic, or encapsulating database queries. Triggers on requests involving specification pattern, ISpecification interface, query builders (Where/Include/OrderBy/Skip/Take), RepositoryBase, EF Core integration, projection/Select, pagination, or reusable query logic. Also use for questions about eliminating custom repository methods, testable queries, or Clean Architecture data access patterns.
---

# Specification Pattern Skill

Implement the Specification pattern for encapsulating query logic using Ardalis.Specification library - a .NET library for building reusable, testable, and composable database queries.

## What is the Specification Pattern?

The Specification pattern encapsulates query logic into discrete, reusable objects. Instead of scattering query criteria throughout your application or embedding them in repositories, specifications provide a centralized way to define complex queries.

**Problems it solves:**
- Duplicated LINQ expressions across the codebase
- Repositories with too many custom query methods
- Query logic mixed with business logic
- Difficulty testing query logic in isolation
- Hidden ORM features behind repository abstractions
- Hard-to-name complex queries

**Benefits:**
- **Centralized query logic**: Keep all query expressions in one place
- **Domain-centric**: Place query definitions in the domain layer
- **Reusable**: Define once, use everywhere
- **Testable**: Unit test specifications independently
- **Named queries**: Use meaningful names instead of cryptic LINQ
- **Type-safe**: Compile-time checking of query expressions

## Installation

```bash
# Core library
dotnet add package Ardalis.Specification

# Entity Framework Core integration
dotnet add package Ardalis.Specification.EntityFrameworkCore

# Entity Framework 6 integration (legacy)
dotnet add package Ardalis.Specification.EntityFramework6
```

**Latest Version**: 9.3.1+ (as of August 2024)
**GitHub**: https://github.com/ardalis/Specification
**Documentation**: https://specification.ardalis.com/

## Basic Usage

### Simple Specification

```csharp
using Ardalis.Specification;

public class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(int id)
    {
        Query.Where(c => c.Id == id);
    }
}

// Usage with repository
var spec = new CustomerByIdSpec(101);
var customer = await repository.FirstOrDefaultAsync(spec);
```

### Specification with Multiple Criteria

```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec()
    {
        Query.Where(c => c.IsActive)
             .Where(c => c.DeletedDate == null)
             .OrderBy(c => c.Name);
    }
}
```

### Specification with Parameters

```csharp
public class CustomersByCountrySpec : Specification<Customer>
{
    public CustomersByCountrySpec(string country)
    {
        Query.Where(c => c.Country == country)
             .OrderBy(c => c.Name);
    }
}

// Usage
var spec = new CustomersByCountrySpec("USA");
var customers = await repository.ListAsync(spec);
```

## Query Builder Features

### Where Clauses

```csharp
public class CustomerSearchSpec : Specification<Customer>
{
    public CustomerSearchSpec(string searchTerm)
    {
        // Multiple Where clauses are combined with AND
        Query.Where(c => c.FirstName.Contains(searchTerm) || 
                        c.LastName.Contains(searchTerm))
             .Where(c => c.IsActive);
    }
}
```

**Conditional Where:**
```csharp
public class CustomerFilterSpec : Specification<Customer>
{
    public CustomerFilterSpec(string? country, bool? isActive)
    {
        // Where with condition - only added if condition is true
        Query.Where(c => c.Country == country, !string.IsNullOrEmpty(country))
             .Where(c => c.IsActive == isActive, isActive.HasValue);
    }
}
```

### Include (Eager Loading)

```csharp
public class CustomerWithAddressesSpec : Specification<Customer>
{
    public CustomerWithAddressesSpec(int id)
    {
        Query.Where(c => c.Id == id)
             .Include(c => c.Addresses);
    }
}
```

**ThenInclude for nested relationships:**
```csharp
public class OrderWithDetailsSpec : Specification<Order>
{
    public OrderWithDetailsSpec(int id)
    {
        Query.Where(o => o.Id == id)
             .Include(o => o.OrderItems)
             .ThenInclude(oi => oi.Product);
    }
}
```

**Multiple Includes:**
```csharp
public class CustomerFullSpec : Specification<Customer>
{
    public CustomerFullSpec(int id)
    {
        Query.Where(c => c.Id == id)
             .Include(c => c.Addresses)
             .Include(c => c.Orders)
                 .ThenInclude(o => o.OrderItems);
    }
}
```

**Include strings (for dynamic queries):**
```csharp
public class DynamicIncludeSpec : Specification<Customer>
{
    public DynamicIncludeSpec(int id, List<string> includes)
    {
        Query.Where(c => c.Id == id);
        
        foreach (var include in includes)
        {
            Query.Include(include);
        }
    }
}
```

### OrderBy and ThenBy

```csharp
public class CustomersSortedSpec : Specification<Customer>
{
    public CustomersSortedSpec()
    {
        Query.OrderBy(c => c.LastName)
             .ThenBy(c => c.FirstName)
             .ThenByDescending(c => c.DateCreated);
    }
}
```

**Conditional Ordering:**
```csharp
public class CustomersDynamicSortSpec : Specification<Customer>
{
    public CustomersDynamicSortSpec(bool sortByName)
    {
        // OrderBy with condition - only added if condition is true
        Query.OrderBy(c => c.Name, sortByName)
             .OrderBy(c => c.Id, !sortByName);
    }
}
```

**Dynamic Ordering with Extension:**
```csharp
public static class CustomerSpecExtensions
{
    public static IOrderedSpecificationBuilder<Customer> ApplyOrdering(
        this ISpecificationBuilder<Customer> builder,
        string sortBy,
        string direction)
    {
        var isAscending = direction?.ToLower() != "desc";
        
        return sortBy?.ToLower() switch
        {
            "firstname" => isAscending 
                ? builder.OrderBy(c => c.FirstName)
                : builder.OrderByDescending(c => c.FirstName),
            "lastname" => isAscending
                ? builder.OrderBy(c => c.LastName)
                : builder.OrderByDescending(c => c.LastName),
            _ => builder.OrderBy(c => c.Id)
        };
    }
}

// Usage
public class CustomerDynamicSpec : Specification<Customer>
{
    public CustomerDynamicSpec(string sortBy, string direction)
    {
        Query.ApplyOrdering(sortBy, direction);
    }
}
```

### Skip and Take (Pagination)

```csharp
public class CustomersPaginatedSpec : Specification<Customer>
{
    public CustomersPaginatedSpec(int page, int pageSize)
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.Name)
             .Skip((page - 1) * pageSize)
             .Take(pageSize);
    }
}

// Usage
var spec = new CustomersPaginatedSpec(page: 2, pageSize: 20);
var customers = await repository.ListAsync(spec);
var totalCount = await repository.CountAsync(spec); // Ignores Skip/Take
```

### Search (Pattern Matching)

```csharp
public class CustomerSearchByTermSpec : Specification<Customer>
{
    public CustomerSearchByTermSpec(string searchTerm)
    {
        // Search is case-insensitive and uses LIKE in SQL
        Query.Search(c => c.FirstName, $"%{searchTerm}%")
             .Search(c => c.LastName, $"%{searchTerm}%")
             .Search(c => c.Email, $"%{searchTerm}%");
    }
}
```

### AsNoTracking

```csharp
public class CustomerReadOnlySpec : Specification<Customer>
{
    public CustomerReadOnlySpec()
    {
        Query.AsNoTracking(); // For read-only queries - better performance
    }
}
```

### AsSplitQuery

```csharp
public class OrderWithItemsSplitSpec : Specification<Order>
{
    public OrderWithItemsSplitSpec(int id)
    {
        Query.Where(o => o.Id == id)
             .Include(o => o.OrderItems)
             .AsSplitQuery(); // Uses separate queries for each collection
    }
}
```

### IgnoreQueryFilters

```csharp
public class AllCustomersIncludingDeletedSpec : Specification<Customer>
{
    public AllCustomersIncludingDeletedSpec()
    {
        Query.IgnoreQueryFilters(); // Ignores global query filters
    }
}
```

## Projections (Select)

### Basic Projection

```csharp
public class CustomerNameProjectionSpec : Specification<Customer, CustomerNameDto>
{
    public CustomerNameProjectionSpec()
    {
        Query.Select(c => new CustomerNameDto
        {
            Id = c.Id,
            FullName = c.FirstName + " " + c.LastName,
            Email = c.Email
        });
    }
}

public class CustomerNameDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Usage
var spec = new CustomerNameProjectionSpec();
var dtos = await repository.ListAsync(spec); // Returns List<CustomerNameDto>
```

### Projection with Filtering

```csharp
public class ActiveCustomerProjectionSpec : Specification<Customer, CustomerDto>
{
    public ActiveCustomerProjectionSpec()
    {
        Query.Where(c => c.IsActive)
             .Select(c => new CustomerDto
             {
                 Id = c.Id,
                 Name = c.FirstName + " " + c.LastName,
                 TotalOrders = c.Orders.Count
             });
    }
}
```

### Reusing Projections with WithProjectionOf

```csharp
// Base specification with filtering
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec()
    {
        Query.Where(c => c.IsActive)
             .OrderBy(c => c.Name);
    }
}

// Reuse with different projection
public class ActiveCustomerNamesSpec : Specification<Customer, CustomerNameDto>
{
    public ActiveCustomerNamesSpec()
    {
        Query.WithProjectionOf(new ActiveCustomersSpec())
             .Select(c => new CustomerNameDto
             {
                 Id = c.Id,
                 FullName = c.FirstName + " " + c.LastName
             });
    }
}
```

## SingleResultSpecification

For queries that should return exactly one result:

```csharp
public class CustomerByIdSpec : SingleResultSpecification<Customer>
{
    public CustomerByIdSpec(int id)
    {
        Query.Where(c => c.Id == id);
    }
}

// Usage - compile-time safety for single results
var spec = new CustomerByIdSpec(101);
var customer = await repository.SingleOrDefaultAsync(spec);
```

## Repository Integration

### Defining Repository Interface

```csharp
using Ardalis.Specification;

public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // Add custom methods if needed
}

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class
{
    // Read-only repository
}
```

### Implementing Repository with EF Core

```csharp
using Ardalis.Specification.EntityFrameworkCore;

public class Repository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    private readonly ApplicationDbContext _dbContext;

    public Repository(ApplicationDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Add custom implementations if needed
}

public class ReadRepository<T> : RepositoryBase<T>, IReadRepository<T> where T : class
{
    public ReadRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}
```

### Repository Methods

```csharp
// Single result
var customer = await repository.GetByIdAsync<int>(id);
var customer = await repository.FirstOrDefaultAsync(spec);
var customer = await repository.SingleOrDefaultAsync(spec);

// Multiple results
var customers = await repository.ListAsync(spec);
var allCustomers = await repository.ListAsync(); // All entities

// Count
var count = await repository.CountAsync(spec);
var totalCount = await repository.CountAsync(); // All entities

// Check existence
var exists = await repository.AnyAsync(spec);

// Add
await repository.AddAsync(customer);
await repository.AddRangeAsync(customers);

// Update
await repository.UpdateAsync(customer);
await repository.UpdateRangeAsync(customers);

// Delete
await repository.DeleteAsync(customer);
await repository.DeleteRangeAsync(customers);

// SaveChanges
await repository.SaveChangesAsync();
```

### Registering in DI Container

```csharp
// Startup.cs or Program.cs
services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
services.AddScoped(typeof(IReadRepository<>), typeof(ReadRepository<>));
```

## Advanced Patterns

### Specification with Post-Processing

```csharp
public class CustomerWithCalculatedDataSpec : Specification<Customer>
{
    public CustomerWithCalculatedDataSpec()
    {
        Query.Where(c => c.IsActive);

        // Post-processing happens in-memory after database query
        Query.PostProcessingAction(customers =>
        {
            foreach (var customer in customers)
            {
                customer.FullName = $"{customer.FirstName} {customer.LastName}";
            }
        });
    }
}
```

### Combining Specifications with Extensions

```csharp
public static class CustomerSpecExtensions
{
    public static ISpecificationBuilder<Customer> ApplyActiveFilter(
        this ISpecificationBuilder<Customer> builder)
    {
        return builder.Where(c => c.IsActive && c.DeletedDate == null);
    }

    public static ISpecificationBuilder<Customer> ApplyCountryFilter(
        this ISpecificationBuilder<Customer> builder,
        string country)
    {
        return builder.Where(c => c.Country == country, !string.IsNullOrEmpty(country));
    }
}

// Usage
public class CustomerByCountrySpec : Specification<Customer>
{
    public CustomerByCountrySpec(string country)
    {
        Query.ApplyActiveFilter()
             .ApplyCountryFilter(country)
             .OrderBy(c => c.Name);
    }
}
```

### Base Specification Class

```csharp
public abstract class BaseSpecification<T> : Specification<T> where T : class
{
    protected void ApplyPaging(int page, int pageSize)
    {
        Query.Skip((page - 1) * pageSize)
             .Take(pageSize);
    }

    protected void ApplyActiveFilter()
    {
        if (typeof(T).GetProperty("IsActive") != null)
        {
            Query.Where("IsActive == true");
        }
    }
}

public class CustomerPaginatedSpec : BaseSpecification<Customer>
{
    public CustomerPaginatedSpec(int page, int pageSize)
    {
        ApplyActiveFilter();
        Query.OrderBy(c => c.Name);
        ApplyPaging(page, pageSize);
    }
}
```

## Common Patterns

### Search and Filter Pattern

```csharp
public class CustomerSearchSpec : Specification<Customer>
{
    public CustomerSearchSpec(CustomerSearchCriteria criteria)
    {
        // Search term
        if (!string.IsNullOrWhiteSpace(criteria.SearchTerm))
        {
            Query.Search(c => c.FirstName, $"%{criteria.SearchTerm}%")
                 .Search(c => c.LastName, $"%{criteria.SearchTerm}%")
                 .Search(c => c.Email, $"%{criteria.SearchTerm}%");
        }

        // Filters
        Query.Where(c => c.Country == criteria.Country, !string.IsNullOrEmpty(criteria.Country))
             .Where(c => c.IsActive == criteria.IsActive, criteria.IsActive.HasValue)
             .Where(c => c.DateCreated >= criteria.FromDate, criteria.FromDate.HasValue)
             .Where(c => c.DateCreated <= criteria.ToDate, criteria.ToDate.HasValue);

        // Sorting
        ApplySorting(criteria.SortBy, criteria.SortDirection);

        // Pagination
        if (criteria.IsPagingEnabled)
        {
            Query.Skip((criteria.Page - 1) * criteria.PageSize)
                 .Take(criteria.PageSize);
        }
    }

    private void ApplySorting(string sortBy, string direction)
    {
        var isAscending = direction?.ToLower() != "desc";

        switch (sortBy?.ToLower())
        {
            case "name":
                if (isAscending)
                    Query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName);
                else
                    Query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName);
                break;
            default:
                Query.OrderBy(c => c.Id);
                break;
        }
    }
}

public class CustomerSearchCriteria
{
    public string? SearchTerm { get; set; }
    public string? Country { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string SortBy { get; set; } = "Id";
    public string SortDirection { get; set; } = "asc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public bool IsPagingEnabled => Page > 0 && PageSize > 0;
}
```

### Aggregate Root with Related Data

```csharp
public class OrderWithFullDetailsSpec : Specification<Order>
{
    public OrderWithFullDetailsSpec(int orderId)
    {
        Query.Where(o => o.Id == orderId)
             .Include(o => o.Customer)
             .Include(o => o.OrderItems)
                 .ThenInclude(oi => oi.Product)
             .Include(o => o.OrderItems)
                 .ThenInclude(oi => oi.Product)
                     .ThenInclude(p => p.Category)
             .Include(o => o.ShippingAddress);
    }
}
```

### Soft Delete Pattern

```csharp
public class ActiveEntitiesSpec<T> : Specification<T> where T : class, ISoftDelete
{
    public ActiveEntitiesSpec()
    {
        Query.Where(e => e.DeletedDate == null);
    }
}

public interface ISoftDelete
{
    DateTime? DeletedDate { get; set; }
}

// Usage
public class ActiveCustomersSpec : ActiveEntitiesSpec<Customer>
{
    public ActiveCustomersSpec()
    {
        Query.OrderBy(c => c.Name);
    }
}
```

## Best Practices

### 1. Name Specifications Descriptively

```csharp
// ✅ Good - clear intent
public class ActiveCustomersByCountrySpec : Specification<Customer> { }
public class OrdersShippedLastMonthSpec : Specification<Order> { }

// ❌ Bad - generic name
public class CustomerSpec : Specification<Customer> { }
public class GetOrdersSpec : Specification<Order> { }
```

### 2. Keep Specifications Focused

```csharp
// ✅ Good - single responsibility
public class CustomerByIdSpec : Specification<Customer>
{
    public CustomerByIdSpec(int id)
    {
        Query.Where(c => c.Id == id);
    }
}

// ❌ Bad - too many responsibilities
public class CustomerMegaSpec : Specification<Customer>
{
    public CustomerMegaSpec(
        int? id, string? name, string? country,
        bool? isActive, DateTime? fromDate, DateTime? toDate,
        int? page, int? pageSize)
    {
        // Too complex, hard to test and reuse
    }
}
```

### 3. Use Parameters for Dynamic Behavior

```csharp
// ✅ Good - parameterized
public class CustomersByStatusSpec : Specification<Customer>
{
    public CustomersByStatusSpec(bool isActive)
    {
        Query.Where(c => c.IsActive == isActive);
    }
}
```

### 4. Place in Domain Layer

```
MyProject.Core/              (Domain Layer)
├── Entities/
│   └── Customer.cs
├── Specifications/
│   ├── CustomerByIdSpec.cs
│   ├── ActiveCustomersSpec.cs
│   └── CustomersByCountrySpec.cs
└── Interfaces/
    └── IRepository.cs

MyProject.Infrastructure/    (Infrastructure Layer)
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Repository.cs
└── Migrations/
```

### 5. Test Specifications Independently

```csharp
[Fact]
public void CustomerByIdSpec_FiltersById()
{
    // Arrange
    var spec = new CustomerByIdSpec(101);

    // Act
    var result = spec.Evaluate(GetTestCustomers()).ToList();

    // Assert
    Assert.Single(result);
    Assert.Equal(101, result.First().Id);
}
```

### 6. Use SingleResultSpecification for Single Results

```csharp
// ✅ Good - explicit single result
public class CustomerByIdSpec : SingleResultSpecification<Customer>
{
    public CustomerByIdSpec(int id)
    {
        Query.Where(c => c.Id == id);
    }
}

// Usage
var customer = await repository.SingleOrDefaultAsync(spec); // Type-safe
```

### 7. Avoid Specification Composition

```csharp
// ❌ Discouraged - specification composition
var spec1 = new ActiveCustomersSpec();
var spec2 = new CustomersByCountrySpec("USA");
// No built-in way to combine these

// ✅ Better - use extension methods
public static class CustomerSpecExtensions
{
    public static ISpecificationBuilder<Customer> WithActiveFilter(
        this ISpecificationBuilder<Customer> builder)
    {
        return builder.Where(c => c.IsActive);
    }
}
```

## Troubleshooting

### Issue: Skip/Take Not Working

**Problem**: Pagination (Skip/Take) not working correctly.

**Solution**: Ensure OrderBy is applied before Skip/Take:
```csharp
// ✅ Correct - OrderBy before Skip/Take
Query.Where(c => c.IsActive)
     .OrderBy(c => c.Id)  // Required for deterministic paging
     .Skip(skip)
     .Take(take);

// ❌ Wrong - no OrderBy
Query.Where(c => c.IsActive)
     .Skip(skip)
     .Take(take);
```

### Issue: Include Not Loading Related Data

**Problem**: Include not loading navigation properties.

**Solution**: Ensure navigation properties are virtual for lazy loading:
```csharp
public class Customer
{
    public int Id { get; set; }
    public virtual ICollection<Order> Orders { get; set; } // virtual keyword
}
```

### Issue: CountAsync Includes Pagination

**Problem**: CountAsync returns page count instead of total count.

**Solution**: CountAsync automatically ignores Skip/Take:
```csharp
var spec = new CustomersPaginatedSpec(page: 1, pageSize: 10);
var items = await repository.ListAsync(spec);      // 10 items
var total = await repository.CountAsync(spec);     // Total count (ignores Skip/Take)
```

### Issue: Specification Not Evaluated

**Problem**: Specification query not applied to database.

**Solution**: Ensure using correct repository method:
```csharp
// ✅ Correct - spec is evaluated
var customers = await repository.ListAsync(spec);

// ❌ Wrong - spec not used
var customers = await repository.ListAsync();
```

## When to Use Specifications

### ✅ Use Specifications When:
- You have reusable query logic
- Queries are complex with multiple conditions
- Using repository pattern
- Need to test query logic independently
- Want to keep queries in domain layer
- Need named, self-documenting queries

### ❌ Don't Use Specifications When:
- Simple one-off queries (use LINQ directly)
- No repository abstraction needed
- Performance-critical scenarios requiring raw SQL
- Query logic is too simple to warrant abstraction

## Migration from Direct LINQ

### Before (Direct LINQ)

```csharp
public class CustomerService
{
    private readonly DbContext _context;

    public async Task<List<Customer>> GetActiveCustomersByCountry(string country)
    {
        return await _context.Customers
            .Where(c => c.IsActive)
            .Where(c => c.Country == country)
            .Include(c => c.Addresses)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
```

### After (Specification Pattern)

```csharp
// Specification in Domain layer
public class ActiveCustomersByCountrySpec : Specification<Customer>
{
    public ActiveCustomersByCountrySpec(string country)
    {
        Query.Where(c => c.IsActive)
             .Where(c => c.Country == country)
             .Include(c => c.Addresses)
             .OrderBy(c => c.Name);
    }
}

// Service using specification
public class CustomerService
{
    private readonly IRepository<Customer> _repository;

    public async Task<List<Customer>> GetActiveCustomersByCountry(string country)
    {
        var spec = new ActiveCustomersByCountrySpec(country);
        return await _repository.ListAsync(spec);
    }
}
```

## References

- GitHub Repository: https://github.com/ardalis/Specification
- Documentation: https://specification.ardalis.com/
- NuGet Package: https://www.nuget.org/packages/Ardalis.Specification
- Latest Version: 9.3.1
- License: MIT
- Sample Application: eShopOnWeb (https://github.com/dotnet-architecture/eShopOnWeb)
- Clean Architecture Template: https://github.com/ardalis/CleanArchitecture
