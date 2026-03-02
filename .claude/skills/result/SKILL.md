---
name: result
description: Ardalis Result pattern library for .NET. Use when implementing result pattern instead of exceptions, handling API responses, returning success/failure from services, or avoiding exception-based flow control. Triggers on requests involving Result<T>, ResultStatus, returning NotFound/Invalid/Error/Success from services, ASP.NET Core integration with ToActionResult/ToMinimalApiResult, FluentValidation integration, Railway Oriented Programming (Map/Bind), or translating results to HTTP status codes. Also use for questions about avoiding exceptions for control flow, service layer error handling, or Clean Architecture return types.
---

# Result Pattern Skill

Implement the Result pattern using Ardalis.Result library - a .NET abstraction for returning success and multiple failure types from services without using exceptions for control flow.

## What is the Result Pattern?

The Result pattern provides a standard, reusable way to return both success and non-success responses from .NET services that can be easily mapped to API response types (HTTP status codes).

**Problems it solves:**
- Using exceptions for expected business errors (NotFound, validation failures)
- Exception handling overhead and performance costs
- Try-catch blocks cluttering API endpoints
- Unclear method signatures (what failures can occur?)
- Inconsistent error response formats across APIs
- Using exceptions for control flow (anti-pattern)

**Benefits:**
- **Explicit outcomes**: Method signature shows possible results
- **No exception overhead**: Better performance for expected failures
- **Type-safe**: Compile-time checking of result handling
- **Clean endpoints**: No try-catch blocks needed
- **Consistent responses**: Standard error formats
- **Railway Oriented Programming**: Chain operations with Map/Bind

## Installation

```bash
# Core library (no ASP.NET dependencies)
dotnet add package Ardalis.Result

# ASP.NET Core integration
dotnet add package Ardalis.Result.AspNetCore

# FluentValidation integration
dotnet add package Ardalis.Result.FluentValidation
```

**Latest Version**: 10.1.0+ (as of October 2024)
**GitHub**: https://github.com/ardalis/Result
**Documentation**: https://result.ardalis.com/
**NuGet Downloads**: 40M+ total

## Basic Usage

### Simple Success/Failure

```csharp
using Ardalis.Result;

public class CustomerService
{
    public Result<Customer> GetCustomer(int id)
    {
        var customer = _repository.GetById(id);
        
        if (customer == null)
        {
            return Result<Customer>.NotFound();
        }
        
        return Result<Customer>.Success(customer);
    }
}

// Usage
var result = customerService.GetCustomer(101);
if (result.IsSuccess)
{
    var customer = result.Value;
    // Use customer
}
else
{
    // Handle failure based on result.Status
}
```

### Non-Generic Result

```csharp
public Result DeleteCustomer(int id)
{
    if (!_repository.Exists(id))
    {
        return Result.NotFound($"Customer {id} not found");
    }
    
    _repository.Delete(id);
    return Result.Success();
}
```

## Result Status Types

### Success

```csharp
// Generic - with value
return Result<Customer>.Success(customer);
return new Result<Customer>(customer);

// Non-generic - void operation
return Result.Success();
return Result.Success("Operation completed successfully");
```

### NotFound (404)

```csharp
// Generic
return Result<Customer>.NotFound();
return Result<Customer>.NotFound("Customer not found");

// Non-generic
return Result.NotFound();
return Result.NotFound($"Customer {id} not found");
```

### Invalid (400 - Validation Errors)

```csharp
// With validation errors
var validationErrors = new List<ValidationError>
{
    new ValidationError
    {
        Identifier = nameof(Customer.Email),
        ErrorMessage = "Email is required",
        ErrorCode = "EMAIL_REQUIRED",
        Severity = ValidationSeverity.Error
    },
    new ValidationError
    {
        Identifier = nameof(Customer.Age),
        ErrorMessage = "Age must be at least 18",
        ErrorCode = "AGE_MIN",
        Severity = ValidationSeverity.Warning
    }
};

return Result<Customer>.Invalid(validationErrors);

// Simple validation error
return Result.Invalid(new ValidationError
{
    ErrorMessage = "Invalid input"
});
```

### Error (500 - Server Errors)

```csharp
// Generic
return Result<Customer>.Error("Database connection failed");
return Result<Customer>.Error(new[] { "Error 1", "Error 2" });

// Non-generic
return Result.Error("An unexpected error occurred");

// With correlation ID
var errorList = new ErrorList(
    errorMessages: new[] { "Database error" },
    correlationId: "req-12345"
);
return Result.Error(errorList);
```

### Forbidden (403)

```csharp
return Result<Customer>.Forbidden();
return Result<Customer>.Forbidden("You don't have permission to access this resource");
```

### Unauthorized (401)

```csharp
return Result<Customer>.Unauthorized();
return Result<Customer>.Unauthorized("Authentication required");
```

### Conflict (409)

```csharp
return Result<Customer>.Conflict();
return Result<Customer>.Conflict("Email already exists");
```

### Unavailable (503)

```csharp
return Result<Customer>.Unavailable();
return Result<Customer>.Unavailable("Service temporarily unavailable");
```

## Checking Result Status

```csharp
var result = customerService.GetCustomer(101);

// Check success
if (result.IsSuccess)
{
    var customer = result.Value;
}

// Check specific status
if (result.Status == ResultStatus.NotFound)
{
    // Handle not found
}

// Switch on status
switch (result.Status)
{
    case ResultStatus.Ok:
        // Handle success
        break;
    case ResultStatus.NotFound:
        // Handle not found
        break;
    case ResultStatus.Invalid:
        // Handle validation errors
        var errors = result.ValidationErrors;
        break;
    case ResultStatus.Error:
        // Handle errors
        var errorMessages = result.Errors;
        break;
}
```

## ASP.NET Core Integration

### Using TranslateResultToActionResult Attribute

```csharp
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;

    [HttpGet("{id}")]
    [TranslateResultToActionResult]
    public Result<CustomerDto> GetCustomer(int id)
    {
        return _customerService.GetCustomer(id);
    }
    
    [HttpPost]
    [TranslateResultToActionResult]
    [ExpectedFailures(ResultStatus.Invalid)]
    public Result<CustomerDto> CreateCustomer(CreateCustomerDto dto)
    {
        return _customerService.CreateCustomer(dto);
    }
    
    [HttpDelete("{id}")]
    [TranslateResultToActionResult]
    public Result DeleteCustomer(int id)
    {
        return _customerService.DeleteCustomer(id);
    }
}
```

**Default HTTP Status Code Mapping:**
- `ResultStatus.Ok` → 200 OK
- `ResultStatus.NotFound` → 404 Not Found
- `ResultStatus.Invalid` → 400 Bad Request
- `ResultStatus.Error` → 500 Internal Server Error
- `ResultStatus.Forbidden` → 403 Forbidden
- `ResultStatus.Unauthorized` → 401 Unauthorized
- `ResultStatus.Conflict` → 409 Conflict
- `ResultStatus.Unavailable` → 503 Service Unavailable

### Using ToActionResult Extension Method

```csharp
[HttpGet("{id}")]
public ActionResult<CustomerDto> GetCustomer(int id)
{
    var result = _customerService.GetCustomer(id);
    return this.ToActionResult(result);
    
    // Alternative syntax
    // return result.ToActionResult(this);
}
```

### Minimal API Integration

```csharp
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/customers/{id}", (int id, ICustomerService service) =>
{
    var result = service.GetCustomer(id);
    return result.ToMinimalApiResult();
});

app.MapPost("/customers", (CreateCustomerDto dto, ICustomerService service) =>
{
    var result = service.CreateCustomer(dto);
    return result.ToMinimalApiResult();
});

app.Run();
```

### Custom Result Conventions

```csharp
// Startup.cs or Program.cs
services.AddControllers(mvcOptions =>
    mvcOptions.AddResultConvention(resultStatusMap =>
        resultStatusMap
            .AddDefaultMap() // Adds default mappings
            .For(ResultStatus.Ok, HttpStatusCode.OK, options => options
                .For("POST", HttpStatusCode.Created)
                .For("DELETE", HttpStatusCode.NoContent))
            .For(ResultStatus.Error, HttpStatusCode.BadRequest, options => options
                .With((controller, result) => 
                    string.Join("\r\n", result.ValidationErrors)))
    )
);
```

## FluentValidation Integration

### Installation

```bash
dotnet add package Ardalis.Result.FluentValidation
```

### Basic Usage

```csharp
using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using FluentValidation;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}

public class CustomerService
{
    public async Task<Result<Customer>> CreateCustomer(CreateCustomerDto dto)
    {
        var validator = new CreateCustomerValidator();
        var validation = await validator.ValidateAsync(dto);
        
        if (!validation.IsValid)
        {
            // Convert FluentValidation errors to Result ValidationErrors
            return Result<Customer>.Invalid(validation.AsErrors());
        }
        
        // Create customer
        var customer = new Customer(dto.Email, dto.FirstName, dto.LastName);
        await _repository.AddAsync(customer);
        
        return Result<Customer>.Success(customer);
    }
}
```

### AsErrors() Extension

The `AsErrors()` extension method converts `ValidationResult` to `List<ValidationError>`:

```csharp
var validation = await validator.ValidateAsync(model);

// FluentValidation ValidationResult → Ardalis ValidationError list
var errors = validation.AsErrors();

// Properties mapped:
// - PropertyName → Identifier
// - ErrorMessage → ErrorMessage
// - ErrorCode → ErrorCode
// - Severity → Severity (Error, Warning, Info)
```

## Railway Oriented Programming

### Map - Transform Success Value

```csharp
var result = customerService.GetCustomer(101)
    .Map(customer => new CustomerDto
    {
        Id = customer.Id,
        FullName = $"{customer.FirstName} {customer.LastName}",
        Email = customer.Email
    });

// If original result is Success, Map transforms the value
// If original result is failure, Map is skipped and failure is returned
```

### Bind - Chain Operations

```csharp
public Result<Order> CreateOrder(int customerId, CreateOrderDto dto)
{
    return customerService.GetCustomer(customerId)
        .Bind(customer => ValidateCustomer(customer))
        .Bind(customer => CreateOrderForCustomer(customer, dto));
}

private Result<Customer> ValidateCustomer(Customer customer)
{
    if (!customer.IsActive)
    {
        return Result<Customer>.Invalid(
            new ValidationError { ErrorMessage = "Customer is not active" });
    }
    return Result<Customer>.Success(customer);
}

private Result<Order> CreateOrderForCustomer(Customer customer, CreateOrderDto dto)
{
    var order = new Order(customer.Id, dto.Items);
    _orderRepository.Add(order);
    return Result<Order>.Success(order);
}
```

### Async Versions

```csharp
// MapAsync
var result = await customerService.GetCustomerAsync(101)
    .MapAsync(async customer => await ConvertToDtoAsync(customer));

// BindAsync
var result = await customerService.GetCustomerAsync(101)
    .BindAsync(async customer => await CreateOrderAsync(customer));
```

### Complex Chaining

```csharp
public async Task<Result<OrderConfirmationDto>> ProcessOrder(int customerId, CreateOrderDto dto)
{
    return await customerService.GetCustomerAsync(customerId)
        .Bind(customer => ValidateCustomerStatus(customer))
        .BindAsync(customer => ValidateInventoryAsync(dto.Items))
        .BindAsync(_ => CreateOrderAsync(customerId, dto))
        .BindAsync(order => ProcessPaymentAsync(order))
        .MapAsync(order => GenerateConfirmationAsync(order));
}

// If any step returns a failure Result, the chain stops
// and that failure is returned
```

## ValidationError Details

```csharp
public class ValidationError
{
    public string Identifier { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public ValidationSeverity Severity { get; set; } = ValidationSeverity.Error;
}

public enum ValidationSeverity
{
    Error,
    Warning,
    Info
}

// Usage
var error = new ValidationError
{
    Identifier = "Email",
    ErrorMessage = "Email is required",
    ErrorCode = "EMAIL_REQUIRED",
    Severity = ValidationSeverity.Error
};
```

## Common Patterns

### Service Layer Pattern

```csharp
public interface ICustomerService
{
    Task<Result<CustomerDto>> GetCustomerAsync(int id);
    Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto);
    Task<Result<CustomerDto>> UpdateCustomerAsync(int id, UpdateCustomerDto dto);
    Task<Result> DeleteCustomerAsync(int id);
}

public class CustomerService : ICustomerService
{
    private readonly IRepository<Customer> _repository;
    private readonly IValidator<CreateCustomerDto> _createValidator;
    private readonly IValidator<UpdateCustomerDto> _updateValidator;

    public async Task<Result<CustomerDto>> GetCustomerAsync(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
        {
            return Result<CustomerDto>.NotFound($"Customer {id} not found");
        }
        
        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var validation = await _createValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result<CustomerDto>.Invalid(validation.AsErrors());
        }

        var customer = new Customer(dto.Email, dto.FirstName, dto.LastName);
        await _repository.AddAsync(customer);
        await _repository.SaveChangesAsync();

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateCustomerAsync(int id, UpdateCustomerDto dto)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
        {
            return Result<CustomerDto>.NotFound($"Customer {id} not found");
        }

        var validation = await _updateValidator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return Result<CustomerDto>.Invalid(validation.AsErrors());
        }

        customer.Update(dto.FirstName, dto.LastName, dto.Email);
        await _repository.UpdateAsync(customer);
        await _repository.SaveChangesAsync();

        return Result<CustomerDto>.Success(MapToDto(customer));
    }

    public async Task<Result> DeleteCustomerAsync(int id)
    {
        var customer = await _repository.GetByIdAsync(id);
        if (customer == null)
        {
            return Result.NotFound($"Customer {id} not found");
        }

        await _repository.DeleteAsync(customer);
        await _repository.SaveChangesAsync();

        return Result.Success();
    }

    private CustomerDto MapToDto(Customer customer) =>
        new CustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FullName = $"{customer.FirstName} {customer.LastName}"
        };
}
```

### CQRS with MediatR

```csharp
// Query
public record GetCustomerQuery(int CustomerId) : IRequest<Result<CustomerDto>>;

public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, Result<CustomerDto>>
{
    private readonly IRepository<Customer> _repository;

    public async Task<Result<CustomerDto>> Handle(
        GetCustomerQuery request, 
        CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.CustomerId);
        if (customer == null)
        {
            return Result<CustomerDto>.NotFound();
        }

        var dto = new CustomerDto
        {
            Id = customer.Id,
            Email = customer.Email,
            FullName = $"{customer.FirstName} {customer.LastName}"
        };

        return Result<CustomerDto>.Success(dto);
    }
}

// Command
public record CreateCustomerCommand(string Email, string FirstName, string LastName) 
    : IRequest<Result<CustomerDto>>;

public class CreateCustomerCommandHandler 
    : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly IRepository<Customer> _repository;
    private readonly IValidator<CreateCustomerCommand> _validator;

    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<CustomerDto>.Invalid(validation.AsErrors());
        }

        var customer = new Customer(request.Email, request.FirstName, request.LastName);
        await _repository.AddAsync(customer);
        await _repository.SaveChangesAsync();

        var dto = new CustomerDto { Id = customer.Id, Email = customer.Email };
        return Result<CustomerDto>.Success(dto);
    }
}

// Controller
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpGet("{id}")]
    [TranslateResultToActionResult]
    public async Task<Result<CustomerDto>> GetCustomer(int id)
    {
        return await _mediator.Send(new GetCustomerQuery(id));
    }

    [HttpPost]
    [TranslateResultToActionResult]
    public async Task<Result<CustomerDto>> CreateCustomer(CreateCustomerCommand command)
    {
        return await _mediator.Send(command);
    }
}
```

### Authorization Pattern

```csharp
public class OrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly ICurrentUserService _currentUser;

    public async Task<Result<Order>> GetOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            return Result<Order>.NotFound();
        }

        // Authorization check
        if (!await _currentUser.IsInRoleAsync("Admin") && 
            order.CustomerId != _currentUser.UserId)
        {
            return Result<Order>.Forbidden("You don't have permission to view this order");
        }

        return Result<Order>.Success(order);
    }

    public async Task<Result> CancelOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null)
        {
            return Result.NotFound();
        }

        if (order.Status == OrderStatus.Shipped)
        {
            return Result.Invalid(new ValidationError
            {
                ErrorMessage = "Cannot cancel a shipped order"
            });
        }

        if (order.CustomerId != _currentUser.UserId)
        {
            return Result.Forbidden();
        }

        order.Cancel();
        await _orderRepository.SaveChangesAsync();

        return Result.Success();
    }
}
```

### Multi-Tenant Pattern

```csharp
public class ProductService
{
    private readonly IRepository<Product> _productRepository;
    private readonly ITenantProvider _tenantProvider;

    public async Task<Result<Product>> GetProductAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null)
        {
            return Result<Product>.NotFound();
        }

        // Ensure product belongs to current tenant
        if (product.TenantId != _tenantProvider.CurrentTenantId)
        {
            return Result<Product>.NotFound(); // Don't reveal existence
        }

        return Result<Product>.Success(product);
    }
}
```

## Best Practices

### 1. Return Result from Services, Not Controllers

```csharp
// ✅ Good - Service returns Result
public class CustomerService
{
    public Result<Customer> GetCustomer(int id)
    {
        // Business logic
        return Result<Customer>.Success(customer);
    }
}

// ❌ Bad - Controller contains business logic
public class CustomersController : ControllerBase
{
    public ActionResult<Customer> GetCustomer(int id)
    {
        var customer = _repository.GetById(id);
        if (customer == null)
            return NotFound();
        return Ok(customer);
    }
}
```

### 2. Use Appropriate Status Types

```csharp
// ✅ Good - Use specific status
if (customer == null)
    return Result<Customer>.NotFound();

if (email.IsInvalid)
    return Result.Invalid(new ValidationError { ErrorMessage = "Invalid email" });

if (!user.HasPermission)
    return Result.Forbidden();

// ❌ Bad - Generic error for everything
if (customer == null)
    return Result<Customer>.Error("Customer not found");
```

### 3. Include Meaningful Error Messages

```csharp
// ✅ Good - Specific, actionable messages
return Result.NotFound($"Customer with ID {customerId} was not found");

return Result.Invalid(new ValidationError
{
    Identifier = nameof(CreateCustomerDto.Email),
    ErrorMessage = "Email address is already in use",
    ErrorCode = "EMAIL_DUPLICATE"
});

// ❌ Bad - Generic messages
return Result.NotFound("Not found");
return Result.Invalid(new ValidationError { ErrorMessage = "Invalid" });
```

### 4. Don't Mix Results and Exceptions

```csharp
// ✅ Good - Consistent use of Result
public Result<Customer> GetCustomer(int id)
{
    var customer = _repository.GetById(id);
    if (customer == null)
        return Result<Customer>.NotFound();
    
    return Result<Customer>.Success(customer);
}

// ❌ Bad - Mixing Results and exceptions
public Result<Customer> GetCustomer(int id)
{
    var customer = _repository.GetById(id);
    if (customer == null)
        throw new NotFoundException(); // Inconsistent!
    
    return Result<Customer>.Success(customer);
}
```

### 5. Use Railway Oriented Programming for Workflows

```csharp
// ✅ Good - Clean chaining
public async Task<Result<OrderDto>> CreateOrder(CreateOrderDto dto)
{
    return await ValidateCustomer(dto.CustomerId)
        .BindAsync(customer => ValidateInventory(dto.Items))
        .BindAsync(_ => CreateOrderEntity(dto))
        .BindAsync(order => ProcessPayment(order))
        .MapAsync(order => MapToDto(order));
}

// ❌ Bad - Nested if statements
public async Task<Result<OrderDto>> CreateOrder(CreateOrderDto dto)
{
    var customerResult = await ValidateCustomer(dto.CustomerId);
    if (!customerResult.IsSuccess) return Result<OrderDto>.Invalid(...);
    
    var inventoryResult = await ValidateInventory(dto.Items);
    if (!inventoryResult.IsSuccess) return Result<OrderDto>.Invalid(...);
    
    // ... more nesting
}
```

### 6. Handle All Result Statuses in Controllers

```csharp
// ✅ Good - Using attribute (handles all statuses)
[HttpGet("{id}")]
[TranslateResultToActionResult]
public Result<CustomerDto> GetCustomer(int id)
{
    return _customerService.GetCustomer(id);
}

// ❌ Bad - Manual handling that might miss cases
[HttpGet("{id}")]
public ActionResult<CustomerDto> GetCustomer(int id)
{
    var result = _customerService.GetCustomer(id);
    if (result.IsSuccess) return Ok(result.Value);
    if (result.Status == ResultStatus.NotFound) return NotFound();
    // What about Invalid? Error? Forbidden?
    return BadRequest();
}
```

### 7. Use ExpectedFailures Attribute

```csharp
// ✅ Good - Documents expected failures
[HttpPost]
[TranslateResultToActionResult]
[ExpectedFailures(ResultStatus.Invalid, ResultStatus.Conflict)]
public Result<CustomerDto> CreateCustomer(CreateCustomerDto dto)
{
    return _customerService.CreateCustomer(dto);
}
```

## Troubleshooting

### Issue: Result Not Converting to HTTP Status

**Problem**: Result object serialized as JSON instead of converted to HTTP status.

**Solution**: Ensure you're using `[TranslateResultToActionResult]` attribute or `.ToActionResult()`:

```csharp
// ✅ With attribute
[TranslateResultToActionResult]
public Result<Customer> GetCustomer(int id) { }

// ✅ With extension method
public ActionResult<Customer> GetCustomer(int id)
{
    return _service.GetCustomer(id).ToActionResult(this);
}

// ❌ Missing translation
public Result<Customer> GetCustomer(int id) { } // Returns JSON!
```

### Issue: ValidationErrors Not Appearing in Response

**Problem**: ValidationErrors not showing in API response.

**Solution**: Configure result conventions or check response body:

```csharp
services.AddControllers(mvcOptions =>
    mvcOptions.AddResultConvention(resultStatusMap =>
        resultStatusMap.AddDefaultMap()
    )
);
```

### Issue: Async Result Methods Not Working

**Problem**: MapAsync/BindAsync not compiling.

**Solution**: Ensure you're awaiting the result:

```csharp
// ✅ Correct
var result = await customerService.GetCustomerAsync(id)
    .MapAsync(customer => ConvertToDtoAsync(customer));

// ❌ Wrong - missing await
var result = customerService.GetCustomerAsync(id)
    .MapAsync(customer => ConvertToDtoAsync(customer));
```

## When to Use Result Pattern

### ✅ Use Result When:
- Service layer methods that can fail in expected ways
- API endpoints returning data with possible failures
- Business logic validation
- Domain service operations
- Authorization checks
- CQRS command/query handlers

### ❌ Don't Use Result When:
- Truly exceptional conditions (use exceptions)
- Infrastructure failures (database down, network errors)
- Programming errors (null reference, index out of bounds)
- Operations that should never fail

## Migration from Exception-Based Approach

### Before (Exceptions)

```csharp
public class CustomerService
{
    public Customer GetCustomer(int id)
    {
        var customer = _repository.GetById(id);
        if (customer == null)
            throw new NotFoundException($"Customer {id} not found");
        
        return customer;
    }
}

[HttpGet("{id}")]
public ActionResult<Customer> GetCustomer(int id)
{
    try
    {
        var customer = _customerService.GetCustomer(id);
        return Ok(customer);
    }
    catch (NotFoundException)
    {
        return NotFound();
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}
```

### After (Result Pattern)

```csharp
public class CustomerService
{
    public Result<Customer> GetCustomer(int id)
    {
        var customer = _repository.GetById(id);
        if (customer == null)
            return Result<Customer>.NotFound($"Customer {id} not found");
        
        return Result<Customer>.Success(customer);
    }
}

[HttpGet("{id}")]
[TranslateResultToActionResult]
public Result<Customer> GetCustomer(int id)
{
    return _customerService.GetCustomer(id);
}
```

## References

- GitHub Repository: https://github.com/ardalis/Result
- Documentation: https://result.ardalis.com/
- NuGet Package: https://www.nuget.org/packages/Ardalis.Result
- ASP.NET Core Package: https://www.nuget.org/packages/Ardalis.Result.AspNetCore
- FluentValidation Package: https://www.nuget.org/packages/Ardalis.Result.FluentValidation
- Latest Version: 10.1.0
- License: MIT
- Blog Post: https://ardalis.com/avoid-using-exceptions-determine-api-status/
