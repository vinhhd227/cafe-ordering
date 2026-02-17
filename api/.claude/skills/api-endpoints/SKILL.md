---
name: apiendpoints
description: Ardalis ApiEndpoints library for ASP.NET Core - REPR pattern implementation for MVC Controllers. Use when implementing Request-Endpoint-Response pattern, organizing API endpoints as single-responsibility classes, replacing bloated MVC controllers, or using vertical slice architecture with Controllers. Triggers on requests involving REPR pattern, EndpointBaseSync, EndpointBaseAsync, WithRequest, WithResponse, WithActionResult, single endpoint per class, or migrating from MVC Controllers to focused endpoints. Note - For .NET 6+ projects, FastEndpoints (built on Minimal APIs) is recommended over ApiEndpoints. This library is primarily for .NET Framework/older ASP.NET Core projects still using MVC Controllers.
---

# ApiEndpoints Skill

Implement the REPR (Request-Endpoint-Response) pattern using Ardalis.ApiEndpoints - a library for creating focused, single-responsibility API endpoints as an alternative to bloated MVC Controllers.

## ⚠️ Important Notice

**For .NET 6+ projects**, use **FastEndpoints** instead! It's built on Minimal APIs and offers better performance and features. ApiEndpoints is primarily for:
- Legacy .NET Framework projects
- Existing ASP.NET Core MVC projects (pre-.NET 6)
- Teams not ready to migrate to Minimal APIs

## What is REPR Pattern?

**REPR** (pronounced "reaper") stands for **Request-Endpoint-Response**:
- **Request**: Input DTO/model
- **Endpoint**: Single class handling one API operation
- **Response**: Output DTO/model

Instead of MVC Controllers with multiple actions, each endpoint is a separate class focused on one operation.

**Problems with MVC Controllers:**
- **Bloated classes**: Controllers grow to hundreds/thousands of lines
- **Lack of cohesion**: Multiple unrelated actions in one controller
- **Difficult to maintain**: Hard to find specific endpoint logic
- **Poor organization**: Models scattered across folders
- **SRP violation**: Single controller handling many responsibilities

**Benefits of REPR:**
- **Single Responsibility**: One class per endpoint
- **Better organization**: Request/Endpoint/Response grouped together
- **Easier testing**: Focused, testable units
- **Feature folders**: Natural fit for vertical slice architecture
- **Reduced friction**: Easy to find and modify specific endpoints

## Installation

```bash
dotnet add package Ardalis.ApiEndpoints
```

**Latest Version**: 4.1.0
**Downloads**: 4.7M+
**GitHub**: https://github.com/ardalis/ApiEndpoints
**Documentation**: https://apiendpoints.ardalis.com/

## Basic Usage

### Before (MVC Controller)

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
    {
        // Logic...
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> CreateCustomer(CreateCustomerRequest request)
    {
        // Logic...
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> UpdateCustomer(int id, UpdateCustomerRequest request)
    {
        // Logic...
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCustomer(int id)
    {
        // Logic...
    }
}
```

### After (ApiEndpoints - REPR)

Each operation in its own class:

```csharp
// Features/Customers/Get/GetCustomerEndpoint.cs
public class GetCustomerEndpoint : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<CustomerDto>
{
    private readonly ICustomerRepository _repository;

    public GetCustomerEndpoint(ICustomerRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("/api/customers/{id}")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(int id, CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        return Ok(new CustomerDto { Id = customer.Id, Name = customer.Name });
    }
}

// Features/Customers/Create/CreateCustomerEndpoint.cs
public class CreateCustomerEndpoint : EndpointBaseAsync
    .WithRequest<CreateCustomerRequest>
    .WithActionResult<CustomerDto>
{
    [HttpPost("/api/customers")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(
        CreateCustomerRequest request, 
        CancellationToken ct = default)
    {
        // Logic...
    }
}
```

## Base Classes

### EndpointBaseSync (Synchronous)

For synchronous operations:

```csharp
public class MyEndpoint : EndpointBaseSync
    .WithRequest<MyRequest>
    .WithActionResult<MyResponse>
{
    [HttpPost("/api/my-endpoint")]
    public override ActionResult<MyResponse> Handle(MyRequest request)
    {
        // Synchronous logic
        return Ok(new MyResponse());
    }
}
```

### EndpointBaseAsync (Asynchronous)

For asynchronous operations (recommended):

```csharp
public class MyEndpoint : EndpointBaseAsync
    .WithRequest<MyRequest>
    .WithActionResult<MyResponse>
{
    [HttpPost("/api/my-endpoint")]
    public override async Task<ActionResult<MyResponse>> HandleAsync(
        MyRequest request,
        CancellationToken ct = default)
    {
        // Async logic
        await Task.Delay(100, ct);
        return Ok(new MyResponse());
    }
}
```

## Fluent Generics Pattern

### WithRequest<TRequest>

Endpoint accepts input:

```csharp
public class CreateCustomerEndpoint : EndpointBaseAsync
    .WithRequest<CreateCustomerRequest>
    .WithActionResult<CustomerDto>
{
    [HttpPost("/api/customers")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(
        CreateCustomerRequest request,
        CancellationToken ct = default)
    {
        // Use request
    }
}

public class CreateCustomerRequest
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}
```

### WithoutRequest

Endpoint takes no input:

```csharp
public class GetAllCustomersEndpoint : EndpointBaseAsync
    .WithoutRequest
    .WithActionResult<List<CustomerDto>>
{
    [HttpGet("/api/customers")]
    public override async Task<ActionResult<List<CustomerDto>>> HandleAsync(
        CancellationToken ct = default)
    {
        // No request parameter
    }
}
```

### WithActionResult<TResponse>

Endpoint returns ActionResult<TResponse>:

```csharp
public class MyEndpoint : EndpointBaseAsync
    .WithRequest<MyRequest>
    .WithActionResult<MyResponse>
{
    [HttpPost("/api/endpoint")]
    public override async Task<ActionResult<MyResponse>> HandleAsync(
        MyRequest request,
        CancellationToken ct = default)
    {
        return Ok(new MyResponse());
    }
}
```

### WithResult<TResponse>

Endpoint returns TResponse directly:

```csharp
public class MyEndpoint : EndpointBaseAsync
    .WithRequest<MyRequest>
    .WithResult<MyResponse>
{
    [HttpPost("/api/endpoint")]
    public override async Task<MyResponse> HandleAsync(
        MyRequest request,
        CancellationToken ct = default)
    {
        return new MyResponse(); // Direct return
    }
}
```

## Common Patterns

### GET Endpoint

```csharp
public class GetCustomerEndpoint : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult<CustomerDto>
{
    private readonly IRepository<Customer> _repository;

    public GetCustomerEndpoint(IRepository<Customer> repository)
    {
        _repository = repository;
    }

    [HttpGet("/api/customers/{id}")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer == null)
            return NotFound();

        return Ok(MapToDto(customer));
    }
}
```

### POST Endpoint

```csharp
public class CreateCustomerEndpoint : EndpointBaseAsync
    .WithRequest<CreateCustomerRequest>
    .WithActionResult<CustomerDto>
{
    private readonly IRepository<Customer> _repository;

    [HttpPost("/api/customers")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(
        [FromBody] CreateCustomerRequest request,
        CancellationToken ct = default)
    {
        var customer = new Customer(request.FirstName, request.LastName, request.Email);
        await _repository.AddAsync(customer, ct);

        return CreatedAtAction(
            nameof(GetCustomerEndpoint),
            new { id = customer.Id },
            MapToDto(customer));
    }
}
```

### PUT Endpoint

```csharp
public class UpdateCustomerEndpoint : EndpointBaseAsync
    .WithRequest<UpdateCustomerRequest>
    .WithActionResult<CustomerDto>
{
    [HttpPut("/api/customers/{id}")]
    public override async Task<ActionResult<CustomerDto>> HandleAsync(
        [FromRoute] UpdateCustomerRequest request,
        CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(request.Id, ct);
        if (customer == null)
            return NotFound();

        customer.Update(request.FirstName, request.LastName);
        await _repository.UpdateAsync(customer, ct);

        return Ok(MapToDto(customer));
    }
}

public class UpdateCustomerRequest
{
    [FromRoute] public int Id { get; set; }
    [FromBody] public string FirstName { get; set; }
    [FromBody] public string LastName { get; set; }
}
```

### DELETE Endpoint

```csharp
public class DeleteCustomerEndpoint : EndpointBaseAsync
    .WithRequest<int>
    .WithActionResult
{
    [HttpDelete("/api/customers/{id}")]
    public override async Task<ActionResult> HandleAsync(
        [FromRoute] int id,
        CancellationToken ct = default)
    {
        var customer = await _repository.GetByIdAsync(id, ct);
        if (customer == null)
            return NotFound();

        await _repository.DeleteAsync(customer, ct);
        return NoContent();
    }
}
```

### List/Search Endpoint

```csharp
public class SearchCustomersEndpoint : EndpointBaseAsync
    .WithRequest<CustomerSearchRequest>
    .WithActionResult<PagedResult<CustomerDto>>
{
    [HttpGet("/api/customers")]
    public override async Task<ActionResult<PagedResult<CustomerDto>>> HandleAsync(
        [FromQuery] CustomerSearchRequest request,
        CancellationToken ct = default)
    {
        var spec = new CustomerSearchSpec(request.SearchTerm, request.Page, request.PageSize);
        var customers = await _repository.ListAsync(spec, ct);
        var totalCount = await _repository.CountAsync(spec, ct);

        var result = new PagedResult<CustomerDto>
        {
            Items = customers.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(result);
    }
}

public class CustomerSearchRequest
{
    [FromQuery] public string? SearchTerm { get; set; }
    [FromQuery] public int Page { get; set; } = 1;
    [FromQuery] public int PageSize { get; set; } = 20;
}
```

## Vertical Slice Organization

```
Features/
├── Customers/
│   ├── Create/
│   │   ├── CreateCustomerEndpoint.cs
│   │   ├── CreateCustomerRequest.cs
│   │   └── CustomerDto.cs
│   ├── Get/
│   │   ├── GetCustomerEndpoint.cs
│   │   └── CustomerDto.cs
│   ├── Update/
│   │   ├── UpdateCustomerEndpoint.cs
│   │   ├── UpdateCustomerRequest.cs
│   │   └── CustomerDto.cs
│   ├── Delete/
│   │   └── DeleteCustomerEndpoint.cs
│   └── List/
│       ├── ListCustomersEndpoint.cs
│       ├── CustomerSearchRequest.cs
│       └── CustomerListDto.cs
└── Orders/
    ├── Create/
    ├── Get/
    └── Submit/
```

## Best Practices

### 1. One Endpoint Per File

```csharp
// ✅ Good - one endpoint per file
Features/Customers/Create/CreateCustomerEndpoint.cs
Features/Customers/Get/GetCustomerEndpoint.cs

// ❌ Bad - multiple endpoints in one file
Features/Customers/CustomerEndpoints.cs
```

### 2. Use Meaningful Names

```csharp
// ✅ Good
public class CreateCustomerEndpoint : EndpointBaseAsync { }
public class GetCustomerByIdEndpoint : EndpointBaseAsync { }

// ❌ Bad
public class CustomerEndpoint1 : EndpointBaseAsync { }
public class CEP : EndpointBaseAsync { }
```

### 3. Group Related DTOs

```csharp
// ✅ Good - DTOs close to endpoint
Features/Customers/Create/
  - CreateCustomerEndpoint.cs
  - CreateCustomerRequest.cs
  - CustomerDto.cs

// ❌ Bad - DTOs scattered
DTOs/CreateCustomerRequest.cs
Controllers/CreateCustomerEndpoint.cs
ViewModels/CustomerDto.cs
```

### 4. Use Route Constants

```csharp
// ✅ Good - reusable routes
public static class CustomerRoutes
{
    public const string Base = "/api/customers";
    public const string GetById = Base + "/{id}";
    public const string Create = Base;
}

[HttpGet(CustomerRoutes.GetById)]

// ❌ Bad - hardcoded strings
[HttpGet("/api/customers/{id}")]
```

### 5. Leverage Dependency Injection

```csharp
// ✅ Good - DI in constructor
public class CreateCustomerEndpoint : EndpointBaseAsync
{
    private readonly IRepository<Customer> _repository;
    private readonly IEmailService _emailService;

    public CreateCustomerEndpoint(
        IRepository<Customer> repository,
        IEmailService emailService)
    {
        _repository = repository;
        _emailService = emailService;
    }
}
```

## Migration from Controllers

### Step 1: Identify Endpoints

List all actions in your controller.

### Step 2: Create Endpoint Classes

Create one class per action.

### Step 3: Move Logic

Copy action logic to `HandleAsync` method.

### Step 4: Update Routes

Move `[HttpGet]` etc. attributes to endpoint.

### Step 5: Test

Verify each endpoint works independently.

## Comparison with FastEndpoints

| Feature | ApiEndpoints | FastEndpoints |
|---------|-------------|---------------|
| Base Technology | MVC Controllers | Minimal APIs |
| Performance | Standard | Faster |
| .NET Version | All versions | .NET 6+ |
| Validation | Manual/FluentValidation | Built-in FluentValidation |
| Dependencies | ASP.NET MVC | Minimal |
| Recommended For | Legacy/.NET Framework | New projects |

**Recommendation**: Use **FastEndpoints** for new .NET 6+ projects. Use **ApiEndpoints** only for legacy/existing MVC projects.

## References

- GitHub: https://github.com/ardalis/ApiEndpoints
- Documentation: https://apiendpoints.ardalis.com/
- NuGet: https://www.nuget.org/packages/Ardalis.ApiEndpoints
- REPR Pattern: https://deviq.com/design-patterns/repr-design-pattern
- Article: https://ardalis.com/mvc-controllers-are-dinosaurs-embrace-api-endpoints/
- Latest Version: 4.1.0
- License: MIT
- **For new projects**: Use FastEndpoints instead!
