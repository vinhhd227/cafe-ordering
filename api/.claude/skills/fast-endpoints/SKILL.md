---
name: fastendpoints
description: FastEndpoints library for .NET - a lightweight REST API framework alternative to ASP.NET Controllers and Minimal APIs. Use when implementing REPR pattern (Request-Endpoint-Response), building REST APIs without controllers, organizing endpoints as single-responsibility classes, or using vertical slice architecture. Triggers on requests involving FastEndpoints framework, Endpoint<TRequest,TResponse> base classes, FluentValidation integration, JWT/Cookie authentication, API versioning, endpoint configuration, model binding, dependency injection in endpoints, pre/post processors, event notifications, or Swagger integration. Also use for questions about replacing MVC controllers, performance optimization, or clean endpoint organization.
---

# FastEndpoints Skill

Build high-performance REST APIs using FastEndpoints - a developer-friendly alternative to ASP.NET Controllers and Minimal APIs that follows the REPR (Request-Endpoint-Response) pattern.

## What is FastEndpoints?

FastEndpoints is a lightweight REST API development framework built on top of ASP.NET Core Minimal APIs. It provides the performance benefits of Minimal APIs with better organization and maintainability through the REPR pattern.

**Problems it solves:**
- Controller bloat with multiple actions in one class
- Minimal APIs lack of structure and organization  
- Excessive boilerplate code with attributes
- Difficulty locating and maintaining endpoints
- No built-in validation and mapper conventions
- Complex routing and model binding configurations

**Benefits:**
- **REPR Pattern**: One class per endpoint (Request-Endpoint-Response)
- **Performance**: On par with Minimal APIs, faster than Controllers
- **Auto-discovery**: Endpoints automatically registered
- **Attribute-free**: Clean code without attribute clutter
- **Built-in FluentValidation**: Automatic validation integration
- **Secure by default**: Must explicitly allow anonymous access
- **Vertical Slice Architecture**: Natural fit for feature-based organization

## Installation

```bash
# Core library
dotnet add package FastEndpoints

# Swagger/OpenAPI support
dotnet add package FastEndpoints.Swagger

# Security (JWT/Cookie authentication)
dotnet add package FastEndpoints.Security

# Testing utilities
dotnet add package FastEndpoints.Testing
```

**Latest Version**: 7.2.0+ (January 2026)
**GitHub**: https://github.com/FastEndpoints/FastEndpoints
**Documentation**: https://fast-endpoints.com/
**NuGet Downloads**: 10M+

## Basic Setup

### Program.cs Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Optional: Add Swagger
builder.Services.SwaggerDocument();

var app = builder.Build();

// Use FastEndpoints
app.UseFastEndpoints();

// Optional: Use Swagger
app.UseSwaggerGen();

app.Run();
```

## Endpoint Types

FastEndpoints provides 4 base types:

### 1. Endpoint<TRequest, TResponse>

Both request and response DTOs.

```csharp
public class CreateCustomerRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class CreateCustomerResponse
{
    public int Id { get; set; }
    public string FullName { get; set; }
}

public class CreateCustomerEndpoint : Endpoint<CreateCustomerRequest, CreateCustomerResponse>
{
    public override void Configure()
    {
        Post("/api/customers");
        AllowAnonymous(); // Endpoints are secure by default
    }

    public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
    {
        // Business logic
        var customer = new Customer
        {
            FirstName = req.FirstName,
            LastName = req.LastName,
            Email = req.Email
        };

        await SaveToDatabase(customer);

        await SendAsync(new CreateCustomerResponse
        {
            Id = customer.Id,
            FullName = $"{customer.FirstName} {customer.LastName}"
        }, cancellation: ct);
    }
}
```

### 2. Endpoint<TRequest>

Request DTO only, flexible response.

```csharp
public class GetCustomerRequest
{
    public int Id { get; set; }
}

public class GetCustomerEndpoint : Endpoint<GetCustomerRequest>
{
    public override void Configure()
    {
        Get("/api/customers/{Id}");
    }

    public override async Task HandleAsync(GetCustomerRequest req, CancellationToken ct)
    {
        var customer = await FindCustomer(req.Id);
        
        if (customer == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        await SendAsync(customer, cancellation: ct);
    }
}
```

### 3. EndpointWithoutRequest<TResponse>

No request, typed response.

```csharp
public class HealthCheckResponse
{
    public string Status { get; set; }
    public DateTime Timestamp { get; set; }
}

public class HealthCheckEndpoint : EndpointWithoutRequest<HealthCheckResponse>
{
    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await SendAsync(new HealthCheckResponse
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow
        }, cancellation: ct);
    }
}
```

### 4. EndpointWithoutRequest

No request or response DTOs.

```csharp
public class DeleteCustomerEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/customers/{id}");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<int>("id");
        await DeleteCustomer(id);
        await SendNoContentAsync(ct);
    }
}
```

## Configuration Methods

### HTTP Verbs

```csharp
public override void Configure()
{
    Get("/api/customers");           // GET
    Post("/api/customers");          // POST  
    Put("/api/customers/{id}");      // PUT
    Patch("/api/customers/{id}");    // PATCH
    Delete("/api/customers/{id}");   // DELETE
}
```

### Security

```csharp
public override void Configure()
{
    Post("/api/orders");
    
    // Allow anonymous access (endpoints are secure by default)
    AllowAnonymous();
    
    // Require authentication
    // (default - no need to call explicitly)
    
    // Require specific roles
    Roles("Admin", "Manager");
    
    // Require specific claims
    Claims("Permission", "CreateOrder");
    
    // Require specific permissions
    Permissions("CreateOrder", "ViewOrders");
    
    // Require specific policy
    Policies("OrderManagement");
}
```

### Versioning

```csharp
public class GetCustomerV1 : Endpoint<GetCustomerRequest>
{
    public override void Configure()
    {
        Get("/api/customers/{id}");
        Version(1);
    }
}

public class GetCustomerV2 : Endpoint<GetCustomerRequest>
{
    public override void Configure()
    {
        Get("/api/customers/{id}");
        Version(2);
        DeprecateAt(3); // Mark as deprecated
    }
}
```

### Tags and Groups

```csharp
public override void Configure()
{
    Post("/api/customers");
    
    // OpenAPI tags
    Tags("Customers", "Public API");
    
    // Group multiple endpoints
    Group<CustomerGroup>();
    
    // Summary and description
    Summary(s =>
    {
        s.Summary = "Create a new customer";
        s.Description = "Creates a new customer in the system";
        s.ResponseExamples[200] = new CreateCustomerResponse { Id = 1 };
    });
}
```

## Validation with FluentValidation

### Automatic Validation

Validators are automatically discovered and registered:

```csharp
public class CreateCustomerValidator : Validator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(50);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .Must(BeUniqueEmail)
            .WithMessage("Email already exists");
    }

    private bool BeUniqueEmail(string email)
    {
        // Check database
        return !_repository.EmailExists(email);
    }
}
```

### Validation with Dependencies

```csharp
public class CreateCustomerValidator : Validator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email)
            .MustAsync(async (email, ct) =>
            {
                // Resolve scoped service
                var repo = Resolve<ICustomerRepository>();
                return !await repo.EmailExistsAsync(email, ct);
            })
            .WithMessage("Email already exists");
    }
}
```

### Application Logic Validation

Throw errors from within the endpoint:

```csharp
public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
{
    if (await EmailExists(req.Email))
    {
        AddError(r => r.Email, "Email already in use");
        await SendErrorsAsync(cancellation: ct);
        return;
    }

    // Continue processing
}
```

## Model Binding

### Route Parameters

```csharp
Get("/api/customers/{id}/orders/{orderId}");

// In handler
var customerId = Route<int>("id");
var orderId = Route<int>("orderId");
```

### Query Parameters

```csharp
public class SearchRequest
{
    public string Query { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

Get("/api/search"); // /api/search?query=test&page=2

// Automatically bound to SearchRequest
```

### Request Body (JSON)

```csharp
Post("/api/customers");

// Automatically binds JSON body to TRequest
```

### Form Data

```csharp
public class UploadFileRequest
{
    public IFormFile File { get; set; }
    public string Description { get; set; }
}

public override void Configure()
{
    Post("/api/upload");
    AllowFileUploads(); // Enable form data
}
```

### Headers

```csharp
var authHeader = HttpContext.Request.Headers["Authorization"];
var customHeader = HttpContext.Request.Headers["X-Custom-Header"];
```

### Claims

```csharp
var userId = User.ClaimValue("UserId");
var email = User.ClaimValue("email");
var roles = User.ClaimValues("role");
```

## Dependency Injection

### Property Injection

```csharp
public class CreateCustomerEndpoint : Endpoint<CreateCustomerRequest, CreateCustomerResponse>
{
    public ICustomerRepository Repository { get; set; }  // Auto-injected
    public ILogger<CreateCustomerEndpoint> Logger { get; set; }

    public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
    {
        Logger.LogInformation("Creating customer {Email}", req.Email);
        var customer = await Repository.CreateAsync(req);
        // ...
    }
}
```

### Constructor Injection

```csharp
public class CreateCustomerEndpoint : Endpoint<CreateCustomerRequest, CreateCustomerResponse>
{
    private readonly ICustomerRepository _repository;
    private readonly ILogger<CreateCustomerEndpoint> _logger;

    public CreateCustomerEndpoint(
        ICustomerRepository repository,
        ILogger<CreateCustomerEndpoint> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

### Manual Resolution

```csharp
public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
{
    var emailService = Resolve<IEmailService>();
    await emailService.SendWelcomeEmailAsync(req.Email);
}
```

## Response Methods

```csharp
// 200 OK
await SendAsync(response, cancellation: ct);
await SendOkAsync(response, ct);

// 201 Created
await SendCreatedAtAsync<GetCustomerEndpoint>(
    new { id = customer.Id },
    customer,
    cancellation: ct);

// 202 Accepted
await SendAcceptedAsync("/api/jobs/123", cancellation: ct);

// 204 No Content
await SendNoContentAsync(ct);

// 400 Bad Request
await SendErrorsAsync(cancellation: ct); // Returns validation errors

// 401 Unauthorized
await SendUnauthorizedAsync(ct);

// 403 Forbidden
await SendForbiddenAsync(ct);

// 404 Not Found
await SendNotFoundAsync(ct);

// 409 Conflict
await SendAsync("Resource already exists", statusCode: 409, cancellation: ct);

// Custom status code
await SendAsync(response, statusCode: 418, cancellation: ct);

// Stream file
await SendFileAsync(fileInfo, cancellation: ct);
await SendStreamAsync(stream, fileName, cancellation: ct);

// TypedResults (for union types)
return TypedResults.Ok(response);
return TypedResults.NotFound();
return TypedResults.Problem("Error occurred");
```

## Mappers

### Basic Mapper

```csharp
public class CustomerMapper : Mapper<CreateCustomerRequest, CreateCustomerResponse, Customer>
{
    // Request -> Entity
    public override Customer ToEntity(CreateCustomerRequest r)
    {
        return new Customer
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            Email = r.Email
        };
    }

    // Entity -> Response
    public override CreateCustomerResponse FromEntity(Customer e)
    {
        return new CreateCustomerResponse
        {
            Id = e.Id,
            FullName = $"{e.FirstName} {e.LastName}"
        };
    }
}

// Usage in endpoint
public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
{
    var customer = Map.ToEntity(req);
    await _repository.AddAsync(customer);
    
    var response = Map.FromEntity(customer);
    await SendAsync(response, cancellation: ct);
}
```

### Mapper with Dependencies

```csharp
public class CustomerMapper : Mapper<CreateCustomerRequest, CreateCustomerResponse, Customer>
{
    public IPasswordHasher PasswordHasher { get; set; } // Injected

    public override Customer ToEntity(CreateCustomerRequest r)
    {
        return new Customer
        {
            FirstName = r.FirstName,
            LastName = r.LastName,
            Email = r.Email,
            PasswordHash = PasswordHasher.Hash(r.Password)
        };
    }
}
```

## Authentication & Authorization

### JWT Bearer Authentication

```csharp
// Program.cs
builder.Services
    .AddAuthenticationJwtBearer(s => s.SigningKey = "your-secret-key-here")
    .AddAuthorization();

// Endpoint
public override void Configure()
{
    Post("/api/secure-data");
    // Authenticated by default - no need to specify
}

// Get claims
var userId = User.ClaimValue("sub");
var email = User.ClaimValue("email");
```

### Cookie Authentication

```csharp
builder.Services
    .AddAuthenticationCookie(validFor: TimeSpan.FromHours(2))
    .AddAuthorization();
```

### Login Endpoint

```csharp
public class LoginRequest
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/api/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await AuthenticateUser(req.Username, req.Password);
        
        if (user == null)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var token = JwtBearer.CreateToken(
            signingKey: "your-secret-key",
            expireAt: DateTime.UtcNow.AddHours(2),
            claims: new[]
            {
                ("sub", user.Id.ToString()),
                ("email", user.Email),
                ("role", "User")
            });

        await SendAsync(new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        }, cancellation: ct);
    }
}
```

## Pre & Post Processors

### Pre-Processor (runs before endpoint handler)

```csharp
public class SecurityHeadersProcessor : IGlobalPreProcessor
{
    public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
    {
        ctx.HttpContext.Response.Headers.Add("X-Content-Type-Options", "nosniff");
        ctx.HttpContext.Response.Headers.Add("X-Frame-Options", "DENY");
        return Task.CompletedTask;
    }
}

// Register globally
app.UseFastEndpoints(c => c.Endpoints.Configurator = ep =>
{
    ep.PreProcessors(Order.Before, new SecurityHeadersProcessor());
});
```

### Post-Processor (runs after endpoint handler)

```csharp
public class LoggingPostProcessor : IGlobalPostProcessor
{
    public Task PostProcessAsync(IPostProcessorContext ctx, CancellationToken ct)
    {
        var logger = ctx.HttpContext.Resolve<ILogger<LoggingPostProcessor>>();
        logger.LogInformation("Endpoint {Endpoint} completed", ctx.Request.GetType().Name);
        return Task.CompletedTask;
    }
}
```

### Endpoint-Specific Processors

```csharp
public override void Configure()
{
    Post("/api/customers");
    PreProcessor<ValidationPreProcessor>();
    PostProcessor<AuditLogPostProcessor>();
}
```

## Event Notifications

### Publishing Events

```csharp
public class CustomerCreatedEvent
{
    public int CustomerId { get; set; }
    public string Email { get; set; }
}

public override async Task HandleAsync(CreateCustomerRequest req, CancellationToken ct)
{
    var customer = await CreateCustomer(req);
    
    // Publish event
    await PublishAsync(new CustomerCreatedEvent
    {
        CustomerId = customer.Id,
        Email = customer.Email
    }, cancellation: ct);
    
    await SendAsync(customer, cancellation: ct);
}
```

### Event Handlers

```csharp
public class SendWelcomeEmailHandler : IEventHandler<CustomerCreatedEvent>
{
    public IEmailService EmailService { get; set; }

    public async Task HandleAsync(CustomerCreatedEvent evt, CancellationToken ct)
    {
        await EmailService.SendWelcomeEmailAsync(evt.Email, ct);
    }
}

public class UpdateAnalyticsHandler : IEventHandler<CustomerCreatedEvent>
{
    public IAnalyticsService Analytics { get; set; }

    public async Task HandleAsync(CustomerCreatedEvent evt, CancellationToken ct)
    {
        await Analytics.TrackCustomerCreatedAsync(evt.CustomerId, ct);
    }
}
```

## Vertical Slice Architecture

Organize endpoints by feature:

```
Features/
├── Customers/
│   ├── Create/
│   │   ├── Endpoint.cs
│   │   ├── Request.cs
│   │   ├── Response.cs
│   │   ├── Validator.cs
│   │   └── Mapper.cs
│   ├── Get/
│   │   ├── Endpoint.cs
│   │   └── Response.cs
│   ├── Update/
│   │   ├── Endpoint.cs
│   │   ├── Request.cs
│   │   └── Validator.cs
│   └── Delete/
│       └── Endpoint.cs
├── Orders/
│   ├── Create/
│   ├── Get/
│   └── List/
└── Products/
    ├── Create/
    ├── Get/
    └── Search/
```

## Best Practices

### 1. One Endpoint Per File

```csharp
// ✅ Good
Features/Customers/Create/Endpoint.cs
Features/Customers/Get/Endpoint.cs

// ❌ Bad
Controllers/CustomersController.cs  // Multiple actions
```

### 2. Use Meaningful Route Names

```csharp
// ✅ Good
Get("/api/customers/{id}");
Post("/api/customers");

// ❌ Bad
Get("/c/{id}");
Post("/CreateCust");
```

### 3. Explicit Security

```csharp
// ✅ Good - explicit
AllowAnonymous();

// ❌ Bad - implicit (requires understanding framework defaults)
```

### 4. Leverage FluentValidation

```csharp
// ✅ Good - declarative validation
public class CreateCustomerValidator : Validator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// ❌ Bad - manual checks
if (string.IsNullOrEmpty(req.Email)) { }
```

### 5. Use Mappers for Complex Transformations

```csharp
// ✅ Good - mapper class
var entity = Map.ToEntity(request);

// ❌ Bad - inline mapping
var entity = new Customer { ... };
```

See references/examples.md for complete real-world examples.

## References

- Website: https://fast-endpoints.com/
- GitHub: https://github.com/FastEndpoints/FastEndpoints
- NuGet: https://www.nuget.org/packages/FastEndpoints
- Latest Version: 7.2.0+
- License: MIT
