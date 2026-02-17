# FastEndpoints API Reference

Core API documentation for FastEndpoints.

## Endpoint Base Classes

```csharp
// Both request and response
Endpoint<TRequest, TResponse>

// Request only
Endpoint<TRequest>

// Response only
EndpointWithoutRequest<TResponse>

// No request or response
EndpointWithoutRequest

// Direct interface implementation
IEndpoint
```

## Configuration Methods

```csharp
// HTTP Verbs
Get(string route)
Post(string route)
Put(string route)
Patch(string route)
Delete(string route)

// Security
AllowAnonymous()
Roles(params string[] roles)
Claims(string claimType, params string[] claimValues)
Permissions(params string[] permissions)
Policies(params string[] policies)

// Versioning
Version(int version)
DeprecateAt(int version)

// Organization
Tags(params string[] tags)
Group<TGroup>()
Summary(Action<EndpointSummary> configure)
Description(string description)

// Features
AllowFileUploads()
DontCatchExceptions()
```

## Request Binding

```csharp
Route<T>(string paramName)           // Route parameters
Query<T>(string paramName)            // Query parameters  
Header<T>(string headerName)          // Headers
Form<T>(string fieldName)             // Form fields
HttpContext.Request.Body              // Raw body
```

## Response Methods

```csharp
// Success
SendAsync(TResponse response, CancellationToken ct)
SendOkAsync(TResponse response, CancellationToken ct)
SendCreatedAtAsync<TEndpoint>(object routeValues, TResponse response, CancellationToken ct)
SendAcceptedAsync(string location, CancellationToken ct)
SendNoContentAsync(CancellationToken ct)

// Errors
SendErrorsAsync(CancellationToken ct)
SendUnauthorizedAsync(CancellationToken ct)
SendForbiddenAsync(CancellationToken ct)
SendNotFoundAsync(CancellationToken ct)

// Files
SendFileAsync(FileInfo file, CancellationToken ct)
SendStreamAsync(Stream stream, string fileName, CancellationToken ct)

// Custom
SendAsync(object response, int statusCode, CancellationToken ct)
```

## Validation

```csharp
Validator<TRequest>

RuleFor(x => x.Property)
    .NotEmpty()
    .MaximumLength(int)
    .EmailAddress()
    .Must(Func<T, bool>)
    .MustAsync(Func<T, CancellationToken, Task<bool>>)
    .WithMessage(string)
```

## Dependency Injection

```csharp
// Property injection (auto)
public IService Service { get; set; }

// Constructor injection
public Endpoint(IService service) { }

// Manual resolution
Resolve<IService>()
```

See SKILL.md for complete documentation.
