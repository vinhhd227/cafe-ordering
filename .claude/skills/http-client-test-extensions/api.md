# HttpClientTestExtensions API Reference

All extension methods for HttpClient testing.

## GET Extensions

```csharp
// Get and deserialize JSON to object
Task<T> GetAndDeserializeAsync<T>(string route, ITestOutputHelper? output = null)

// Get and return string
Task<string> GetAndReturnStringAsync(string route, ITestOutputHelper? output = null)

// Get and ensure contains substring
Task GetAndEnsureSubstringAsync(string route, string substring, ITestOutputHelper? output = null)

// Get and ensure 404
Task GetAndEnsureNotFoundAsync(string route, ITestOutputHelper? output = null)

// Get and ensure 400
Task GetAndEnsureBadRequestAsync(string route, ITestOutputHelper? output = null)

// Get and ensure 401
Task GetAndEnsureUnauthorizedAsync(string route, ITestOutputHelper? output = null)

// Get and ensure 403
Task GetAndEnsureForbiddenAsync(string route, ITestOutputHelper? output = null)

// Get and ensure 302 redirect
Task GetAndEnsureRedirectAsync(string route, string expectedLocation, ITestOutputHelper? output = null)
```

## POST Extensions

```csharp
// Post and deserialize response
Task<TResponse> PostAndDeserializeAsync<TRequest, TResponse>(
    string route, TRequest request, ITestOutputHelper? output = null)

// Post and ensure 404
Task PostAndEnsureNotFoundAsync<TRequest>(
    string route, TRequest request, ITestOutputHelper? output = null)

// Post and ensure 400
Task PostAndEnsureBadRequestAsync<TRequest>(
    string route, TRequest request, ITestOutputHelper? output = null)

// Post and ensure 401
Task PostAndEnsureUnauthorizedAsync<TRequest>(
    string route, TRequest request, ITestOutputHelper? output = null)
```

## PUT Extensions

```csharp
// Put and deserialize response
Task<TResponse> PutAndDeserializeAsync<TRequest, TResponse>(
    string route, TRequest request, ITestOutputHelper? output = null)

// Put and ensure 404
Task PutAndEnsureNotFoundAsync<TRequest>(
    string route, TRequest request, ITestOutputHelper? output = null)

// Put and ensure 400
Task PutAndEnsureBadRequestAsync<TRequest>(
    string route, TRequest request, ITestOutputHelper? output = null)
```

## PATCH Extensions

```csharp
// Patch and deserialize response
Task<TResponse> PatchAndDeserializeAsync<TRequest, TResponse>(
    string route, TRequest request, ITestOutputHelper? output = null)
```

## DELETE Extensions

```csharp
// Delete and ensure success (200/204)
Task DeleteAndEnsureSuccessAsync(string route, ITestOutputHelper? output = null)

// Delete and ensure 404
Task DeleteAndEnsureNotFoundAsync(string route, ITestOutputHelper? output = null)
```

## HttpResponseMessage Extensions

```csharp
void EnsureNotFound(this HttpResponseMessage response)
void EnsureBadRequest(this HttpResponseMessage response)
void EnsureUnauthorized(this HttpResponseMessage response)
void EnsureForbidden(this HttpResponseMessage response)
```

## Helper Methods

```csharp
StringContent StringContentFromObject(object obj)
```

See SKILL.md for usage examples.
