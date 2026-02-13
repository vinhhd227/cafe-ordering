# Result Pattern API Reference

Complete API documentation for Ardalis.Result.

## Result<T> Class

Generic result type containing a value of type T.

```csharp
public class Result<T> : Result
{
    public T Value { get; }
    
    // Static factory methods
    public static Result<T> Success(T value);
    public static Result<T> Success(T value, string successMessage);
    
    public static Result<T> NotFound();
    public static Result<T> NotFound(string errorMessage);
    
    public static Result<T> Invalid(ValidationError validationError);
    public static Result<T> Invalid(List<ValidationError> validationErrors);
    
    public static Result<T> Error(string errorMessage);
    public static Result<T> Error(params string[] errorMessages);
    public static Result<T> Error(ErrorList errorList);
    
    public static Result<T> Forbidden();
    public static Result<T> Forbidden(string errorMessage);
    
    public static Result<T> Unauthorized();
    public static Result<T> Unauthorized(string errorMessage);
    
    public static Result<T> Conflict();
    public static Result<T> Conflict(string errorMessage);
    
    public static Result<T> Unavailable();
    public static Result<T> Unavailable(string errorMessage);
    
    // Constructors
    public Result(T value);
    protected Result(ResultStatus status);
}
```

## Result Class (Non-Generic)

Result type for operations that don't return a value.

```csharp
public class Result
{
    public ResultStatus Status { get; protected set; }
    public bool IsSuccess { get; }
    public string SuccessMessage { get; protected set; }
    public string CorrelationId { get; protected set; }
    public string[] Errors { get; protected set; }
    public List<ValidationError> ValidationErrors { get; protected set; }
    
    // Static factory methods
    public static Result Success();
    public static Result Success(string successMessage);
    
    public static Result NotFound();
    public static Result NotFound(string errorMessage);
    
    public static Result Invalid(ValidationError validationError);
    public static Result Invalid(List<ValidationError> validationErrors);
    
    public static Result Error(string errorMessage);
    public static Result Error(params string[] errorMessages);
    public static Result Error(ErrorList errorList);
    
    public static Result Forbidden();
    public static Result Forbidden(string errorMessage);
    
    public static Result Unauthorized();
    public static Result Unauthorized(string errorMessage);
    
    public static Result Conflict();
    public static Result Conflict(string errorMessage);
    
    public static Result Unavailable();
    public static Result Unavailable(string errorMessage);
}
```

## ResultStatus Enum

```csharp
public enum ResultStatus
{
    Ok = 0,           // 200 OK
    Error = 1,        // 500 Internal Server Error
    Forbidden = 2,    // 403 Forbidden
    Unauthorized = 3, // 401 Unauthorized
    Invalid = 4,      // 400 Bad Request
    NotFound = 5,     // 404 Not Found
    Conflict = 6,     // 409 Conflict
    Unavailable = 7   // 503 Service Unavailable
}
```

## ValidationError Class

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
    Error = 0,
    Warning = 1,
    Info = 2
}
```

## ErrorList Class

```csharp
public record ErrorList
{
    public string[] ErrorMessages { get; init; }
    public string CorrelationId { get; init; }
    
    public ErrorList(string[] errorMessages, string correlationId = "");
}
```

## Railway Oriented Programming Extensions

### Map Methods

Transform the value of a successful result.

```csharp
// Synchronous
public static Result<TOut> Map<TIn, TOut>(
    this Result<TIn> result,
    Func<TIn, TOut> mapper);

// Asynchronous
public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
    this Task<Result<TIn>> resultTask,
    Func<TIn, Task<TOut>> mapper);

public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
    this Task<Result<TIn>> resultTask,
    Func<TIn, TOut> mapper);
```

**Examples:**

```csharp
// Sync Map
var result = GetCustomer(id)
    .Map(customer => new CustomerDto { Id = customer.Id });

// Async Map
var result = await GetCustomerAsync(id)
    .MapAsync(customer => MapToDtoAsync(customer));

var result = await GetCustomerAsync(id)
    .MapAsync(customer => new CustomerDto { Id = customer.Id });
```

### Bind Methods

Chain operations that return Results.

```csharp
// Synchronous
public static Result<TOut> Bind<TIn, TOut>(
    this Result<TIn> result,
    Func<TIn, Result<TOut>> binder);

public static Result Bind<TIn>(
    this Result<TIn> result,
    Func<TIn, Result> binder);

// Asynchronous
public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
    this Task<Result<TIn>> resultTask,
    Func<TIn, Task<Result<TOut>>> binder);

public static async Task<Result> BindAsync<TIn>(
    this Task<Result<TIn>> resultTask,
    Func<TIn, Task<Result>> binder);
```

**Examples:**

```csharp
// Sync Bind
var result = GetCustomer(id)
    .Bind(customer => ValidateCustomer(customer))
    .Bind(customer => CreateOrder(customer));

// Async Bind
var result = await GetCustomerAsync(id)
    .BindAsync(customer => ValidateCustomerAsync(customer))
    .BindAsync(customer => CreateOrderAsync(customer));
```

## ASP.NET Core Extensions

### TranslateResultToActionResult Attribute

Automatically converts Result to ActionResult.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class TranslateResultToActionResultAttribute : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context);
}
```

**Usage:**

```csharp
[TranslateResultToActionResult]
[HttpGet("{id}")]
public Result<CustomerDto> GetCustomer(int id) { }

[TranslateResultToActionResult]
public class CustomersController : ControllerBase { }
```

### ExpectedFailures Attribute

Documents expected failure result statuses.

```csharp
[AttributeUsage(AttributeTargets.Method)]
public class ExpectedFailuresAttribute : Attribute
{
    public ResultStatus[] ResultStatuses { get; }
    
    public ExpectedFailuresAttribute(params ResultStatus[] resultStatuses);
}
```

**Usage:**

```csharp
[HttpPost]
[TranslateResultToActionResult]
[ExpectedFailures(ResultStatus.Invalid, ResultStatus.Conflict)]
public Result<CustomerDto> CreateCustomer(CreateCustomerDto dto) { }
```

### ToActionResult Extension

```csharp
public static class ResultExtensions
{
    public static ActionResult<T> ToActionResult<T>(
        this Result<T> result,
        ControllerBase controller);
    
    public static ActionResult ToActionResult(
        this Result result,
        ControllerBase controller);
}
```

**Usage:**

```csharp
[HttpGet("{id}")]
public ActionResult<CustomerDto> GetCustomer(int id)
{
    var result = _service.GetCustomer(id);
    return this.ToActionResult(result);
    // or: return result.ToActionResult(this);
}
```

### ToMinimalApiResult Extension

For Minimal APIs (IResult).

```csharp
public static class MinimalApiResultExtensions
{
    public static IResult ToMinimalApiResult<T>(this Result<T> result);
    public static IResult ToMinimalApiResult(this Result result);
}
```

**Usage:**

```csharp
app.MapGet("/customers/{id}", (int id, ICustomerService service) =>
{
    var result = service.GetCustomer(id);
    return result.ToMinimalApiResult();
});
```

### Result Convention Configuration

```csharp
public static class ResultConventionExtensions
{
    public static void AddResultConvention(
        this MvcOptions mvcOptions,
        Action<ResultStatusMap> configure);
}

public class ResultStatusMap
{
    public ResultStatusMap AddDefaultMap();
    
    public ResultStatusMap For(
        ResultStatus status,
        HttpStatusCode httpStatusCode,
        Action<ResultStatusOptions>? configure = null);
}

public class ResultStatusOptions
{
    public ResultStatusOptions For(
        string httpMethod,
        HttpStatusCode httpStatusCode);
    
    public ResultStatusOptions With(
        Func<ControllerBase, IResult, object> factory);
}
```

**Usage:**

```csharp
services.AddControllers(mvcOptions =>
    mvcOptions.AddResultConvention(resultStatusMap =>
        resultStatusMap
            .AddDefaultMap()
            .For(ResultStatus.Ok, HttpStatusCode.OK, options => options
                .For("POST", HttpStatusCode.Created)
                .For("DELETE", HttpStatusCode.NoContent))
            .For(ResultStatus.Error, HttpStatusCode.BadRequest)
    )
);
```

## FluentValidation Extensions

### AsErrors Extension

Converts FluentValidation ValidationResult to Result ValidationErrors.

```csharp
public static class FluentValidationExtensions
{
    public static List<ValidationError> AsErrors(
        this ValidationResult validationResult);
}
```

**Usage:**

```csharp
var validation = await validator.ValidateAsync(dto);
if (!validation.IsValid)
{
    return Result<Customer>.Invalid(validation.AsErrors());
}
```

**Mapping:**

- `PropertyName` → `Identifier`
- `ErrorMessage` → `ErrorMessage`
- `ErrorCode` → `ErrorCode`
- `Severity` → `Severity` (Error/Warning/Info)

## Common Result Patterns

### Checking Result Status

```csharp
// Boolean check
if (result.IsSuccess) { }

// Enum comparison
if (result.Status == ResultStatus.NotFound) { }

// Switch statement
switch (result.Status)
{
    case ResultStatus.Ok:
        break;
    case ResultStatus.NotFound:
        break;
    case ResultStatus.Invalid:
        var errors = result.ValidationErrors;
        break;
}
```

### Accessing Result Data

```csharp
// Success
if (result.IsSuccess)
{
    var value = result.Value;
    var message = result.SuccessMessage;
}

// Errors
if (result.Status == ResultStatus.Error)
{
    var errors = result.Errors; // string[]
    var correlationId = result.CorrelationId;
}

// Validation Errors
if (result.Status == ResultStatus.Invalid)
{
    var validationErrors = result.ValidationErrors; // List<ValidationError>
    foreach (var error in validationErrors)
    {
        var identifier = error.Identifier;
        var message = error.ErrorMessage;
        var code = error.ErrorCode;
        var severity = error.Severity;
    }
}
```

### Creating Results

```csharp
// Success with value
return Result<Customer>.Success(customer);
return new Result<Customer>(customer);

// Success with message
return Result.Success("Operation completed");

// NotFound
return Result<Customer>.NotFound();
return Result.NotFound("Customer not found");

// Invalid with single error
return Result.Invalid(new ValidationError
{
    Identifier = "Email",
    ErrorMessage = "Email is required"
});

// Invalid with multiple errors
var errors = new List<ValidationError>
{
    new() { Identifier = "Email", ErrorMessage = "Email is required" },
    new() { Identifier = "Name", ErrorMessage = "Name is required" }
};
return Result<Customer>.Invalid(errors);

// Error
return Result.Error("Database error");
return Result<Customer>.Error("Failed to retrieve customer");

// Error with correlation ID
var errorList = new ErrorList(
    errorMessages: new[] { "Error 1", "Error 2" },
    correlationId: "req-12345"
);
return Result.Error(errorList);

// Other statuses
return Result.Forbidden("Access denied");
return Result.Unauthorized("Login required");
return Result.Conflict("Email already exists");
return Result.Unavailable("Service temporarily unavailable");
```

## HTTP Status Code Mappings

Default mappings when using ASP.NET Core integration:

| ResultStatus | Default HTTP Status | Common Usage |
|-------------|-------------------|--------------|
| Ok | 200 OK | Successful operation |
| NotFound | 404 Not Found | Resource doesn't exist |
| Invalid | 400 Bad Request | Validation errors |
| Error | 500 Internal Server Error | Unexpected errors |
| Forbidden | 403 Forbidden | Access denied |
| Unauthorized | 401 Unauthorized | Authentication required |
| Conflict | 409 Conflict | Resource conflict (e.g., duplicate) |
| Unavailable | 503 Service Unavailable | Service temporarily down |

Can be customized using Result Conventions.

## Package Information

**Ardalis.Result**
- Core library with no dependencies
- Target frameworks: .NET Standard 2.0, .NET 6.0, .NET 8.0
- NuGet: https://www.nuget.org/packages/Ardalis.Result

**Ardalis.Result.AspNetCore**
- ASP.NET Core integration
- Depends on: Ardalis.Result, Microsoft.AspNetCore.Mvc.Core
- NuGet: https://www.nuget.org/packages/Ardalis.Result.AspNetCore

**Ardalis.Result.FluentValidation**
- FluentValidation integration
- Depends on: Ardalis.Result, FluentValidation
- NuGet: https://www.nuget.org/packages/Ardalis.Result.FluentValidation

See SKILL.md for complete usage guide and examples.
