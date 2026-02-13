# Specification API Reference

Core API documentation for Ardalis.Specification.

## ISpecification<T> Interface

Main interface for specifications.

```csharp
public interface ISpecification<T>
{
    IEnumerable<WhereExpressionInfo<T>> WhereExpressions { get; }
    IEnumerable<IncludeExpressionInfo> IncludeExpressions { get; }
    IEnumerable<OrderExpressionInfo<T>> OrderExpressions { get; }
    int? Take { get; }
    int? Skip { get; }
    // ... and more
}
```

## Query Builder Methods

### Where
```csharp
Query.Where(expression)
Query.Where(expression, condition)
```

### Include
```csharp
Query.Include(expression)
Query.ThenInclude(expression)
```

### OrderBy
```csharp
Query.OrderBy(expression)
Query.OrderByDescending(expression)
Query.ThenBy(expression)
Query.ThenByDescending(expression)
```

### Pagination
```csharp
Query.Skip(count)
Query.Take(count)
```

### Other
```csharp
Query.Search(expression, searchTerm)
Query.AsNoTracking()
Query.AsSplitQuery()
Query.IgnoreQueryFilters()
```

## Repository Methods

```csharp
Task<T> GetByIdAsync<TId>(TId id)
Task<T> FirstOrDefaultAsync(ISpecification<T> spec)
Task<List<T>> ListAsync(ISpecification<T> spec)
Task<int> CountAsync(ISpecification<T> spec)
Task<bool> AnyAsync(ISpecification<T> spec)
Task AddAsync(T entity)
Task UpdateAsync(T entity)
Task DeleteAsync(T entity)
Task SaveChangesAsync()
```

See SKILL.md for complete API documentation.
