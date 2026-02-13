# FastEndpoints Advanced Examples

Real-world examples and patterns for FastEndpoints.

## Complete CRUD with Vertical Slice Architecture

```csharp
// Features/Customers/Create/Endpoint.cs
public class Endpoint : Endpoint<Request, Response>
{
    public ICustomerRepository Repository { get; set; }

    public override void Configure()
    {
        Post("/api/customers");
        Roles("Admin", "Manager");
        Summary(s => s.Summary = "Create a new customer");
    }

    public override async Task HandleAsync(Request req, CancellationToken ct)
    {
        var customer = Map.ToEntity(req);
        await Repository.AddAsync(customer, ct);
        
        await PublishAsync(new CustomerCreatedEvent
        {
            CustomerId = customer.Id,
            Email = customer.Email
        }, cancellation: ct);

        var response = Map.FromEntity(customer);
        await SendCreatedAtAsync<Get.Endpoint>(
            new { id = customer.Id },
            response,
            cancellation: ct);
    }
}

// Features/Customers/Create/Request.cs
public class Request
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

// Features/Customers/Create/Response.cs
public class Response
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
}

// Features/Customers/Create/Validator.cs
public class Validator : Validator<Request>
{
    public Validator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) =>
            {
                var repo = Resolve<ICustomerRepository>();
                return !await repo.EmailExistsAsync(email, ct);
            })
            .WithMessage("Email already exists");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?[1-9]\d{1,14}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Invalid phone number format");
    }
}

// Features/Customers/Create/Mapper.cs
public class Mapper : Mapper<Request, Response, Customer>
{
    public override Customer ToEntity(Request r) => new()
    {
        FirstName = r.FirstName,
        LastName = r.LastName,
        Email = r.Email,
        Phone = r.Phone,
        CreatedAt = DateTime.UtcNow
    };

    public override Response FromEntity(Customer e) => new()
    {
        Id = e.Id,
        FullName = $"{e.FirstName} {e.LastName}",
        Email = e.Email
    };
}
```

See SKILL.md for complete guide.
