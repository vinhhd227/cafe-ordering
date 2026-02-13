# Specification Pattern Advanced Examples

Advanced patterns and real-world examples for Ardalis.Specification.

## Complete CQRS Implementation

```csharp
// Query
public record GetCustomerByIdQuery(int CustomerId);

public class GetCustomerByIdQueryHandler
{
    private readonly IReadRepository<Customer> _repository;

    public async Task<CustomerDto?> Handle(GetCustomerByIdQuery query)
    {
        var spec = new CustomerByIdWithDetailsSpec(query.CustomerId);
        return await _repository.FirstOrDefaultAsync(spec);
    }
}

// Specification
public class CustomerByIdWithDetailsSpec : Specification<Customer, CustomerDto>
{
    public CustomerByIdWithDetailsSpec(int id)
    {
        Query.Where(c => c.Id == id)
             .Include(c => c.Addresses)
             .Include(c => c.Orders)
             .Select(c => new CustomerDto
             {
                 Id = c.Id,
                 Name = $"{c.FirstName} {c.LastName}",
                 Email = c.Email,
                 TotalOrders = c.Orders.Count,
                 Addresses = c.Addresses.Select(a => new AddressDto
                 {
                     Street = a.Street,
                     City = a.City
                 }).ToList()
             });
    }
}
```

See SKILL.md for complete documentation.
