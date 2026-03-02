# ApiEndpoints API Reference

Core API for Ardalis.ApiEndpoints library.

## Base Classes

```csharp
// Synchronous
EndpointBaseSync

// Asynchronous (recommended)
EndpointBaseAsync
```

## Fluent Generics

```csharp
// With request
.WithRequest<TRequest>

// Without request
.WithoutRequest

// With ActionResult response
.WithActionResult<TResponse>
.WithActionResult

// With direct response
.WithResult<TResponse>
```

## Handle Methods

```csharp
// Sync
public override ActionResult<TResponse> Handle(TRequest request)

// Async
public override async Task<ActionResult<TResponse>> HandleAsync(
    TRequest request, 
    CancellationToken ct = default)
```

## HTTP Attributes

```csharp
[HttpGet("/route")]
[HttpPost("/route")]
[HttpPut("/route")]
[HttpPatch("/route")]
[HttpDelete("/route")]
```

## Parameter Binding

```csharp
[FromRoute] - Route parameters
[FromQuery] - Query string
[FromBody] - Request body
[FromHeader] - Headers
```

See SKILL.md for usage examples.
