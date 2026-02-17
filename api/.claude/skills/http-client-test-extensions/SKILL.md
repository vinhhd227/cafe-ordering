---
name: httpclienttestextensions
description: Ardalis HttpClientTestExtensions library for ASP.NET Core - extension methods for testing APIs with xUnit and WebApplicationFactory. Use when writing integration/functional tests for REST APIs, reducing test boilerplate code, testing HTTP endpoints with HttpClient, deserializing JSON responses, asserting HTTP status codes (200, 400, 401, 403, 404, 302), or simplifying WebApplicationFactory test setup. Triggers on requests involving GetAndDeserialize, GetAndEnsureNotFound, PostAndDeserialize, PutAndDeserialize, functional testing, integration testing, API testing, xUnit ITestOutputHelper, or eliminating test repetition.
---

# HttpClientTestExtensions Skill

Eliminate boilerplate code in ASP.NET Core API integration/functional tests using Ardalis.HttpClientTestExtensions - a collection of extension methods for HttpClient that simplify testing with xUnit.

## What is HttpClientTestExtensions?

A library that provides clean, concise extension methods for testing HTTP endpoints using `HttpClient` and `WebApplicationFactory`. It reduces test code from 5-10 lines down to 1-2 lines.

**Problems it solves:**
- **Repetitive test code**: Every test has similar boilerplate
- **Manual deserialization**: Constantly reading and deserializing JSON
- **Verbose status checks**: Long-winded response validation
- **Poor readability**: Hard to see what's being tested
- **No logging output**: Difficult to debug failing tests

**Benefits:**
- **DRY tests**: One-liner for most common scenarios
- **Automatic deserialization**: Built-in JSON to object conversion
- **Status code helpers**: Named methods for 200, 400, 401, 403, 404, 302
- **xUnit integration**: Optional ITestOutputHelper for logging
- **Better readability**: Tests focus on assertions, not plumbing
- **All HTTP verbs**: GET, POST, PUT, PATCH, DELETE support

## Installation

```bash
dotnet add package Ardalis.HttpClientTestExtensions
```

**Latest Version**: 4.2.0
**Downloads**: 360K+
**GitHub**: https://github.com/ardalis/HttpClientTestExtensions
**License**: MIT
**Test Framework**: xUnit (with ITestOutputHelper support)

## Before vs After

### Before (Verbose)

```csharp
[Fact]
public async Task Returns3Doctors()
{
    // Arrange
    var response = await _client.GetAsync("/api/doctors");
    
    // Act
    response.EnsureSuccessStatusCode();
    var stringResponse = await response.Content.ReadAsStringAsync();
    _outputHelper.WriteLine(stringResponse); // For debugging
    var result = JsonSerializer.Deserialize<ListDoctorResponse>(
        stringResponse, 
        Constants.DefaultJsonOptions);
    
    // Assert
    Assert.Equal(3, result.Doctors.Count());
    Assert.Contains(result.Doctors, x => x.Name == "Dr. Smith");
}
```

### After (Concise)

```csharp
[Fact]
public async Task Returns3Doctors()
{
    var result = await _client.GetAndDeserializeAsync<ListDoctorResponse>(
        "/api/doctors", 
        _outputHelper);
    
    Assert.Equal(3, result.Doctors.Count());
    Assert.Contains(result.Doctors, x => x.Name == "Dr. Smith");
}
```

## Setup

### Test Class with WebApplicationFactory

```csharp
using Ardalis.HttpClientTestExtensions;
using Xunit;
using Xunit.Abstractions;

public class DoctorEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;

    public DoctorEndpointsTests(
        WebApplicationFactory<Program> factory,
        ITestOutputHelper outputHelper)
    {
        _client = factory.CreateClient();
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetDoctors_Returns3Doctors()
    {
        var result = await _client.GetAndDeserializeAsync<ListDoctorResponse>(
            "/api/doctors", 
            _outputHelper);

        Assert.Equal(3, result.Doctors.Count);
    }
}
```

### Custom WebApplicationFactory

```csharp
public class CustomWebApplicationFactory<TProgram> 
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with in-memory database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Seed test data
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            SeedData.Initialize(db);
        });
    }
}
```

## GET Methods

### GetAndDeserializeAsync<T>

Get endpoint and deserialize JSON response to object:

```csharp
[Fact]
public async Task GetDoctor_ReturnsDoctor()
{
    var doctor = await _client.GetAndDeserializeAsync<DoctorDto>(
        "/api/doctors/1",
        _outputHelper);

    Assert.Equal("Dr. Smith", doctor.Name);
    Assert.Equal("Cardiology", doctor.Specialty);
}
```

### GetAndReturnStringAsync

Get endpoint and return response as string:

```csharp
[Fact]
public async Task GetHealthCheck_ReturnsHealthy()
{
    var result = await _client.GetAndReturnStringAsync(
        "/healthcheck",
        _outputHelper);

    Assert.Contains("Healthy", result);
}
```

### GetAndEnsureSubstringAsync

Get endpoint and assert response contains substring:

```csharp
[Fact]
public async Task GetHealthCheck_ContainsStatus()
{
    await _client.GetAndEnsureSubstringAsync(
        "/healthcheck",
        "Status: OK",
        _outputHelper);
}
```

### GetAndEnsureNotFoundAsync

Get endpoint and assert 404 is returned:

```csharp
[Fact]
public async Task GetDoctor_InvalidId_ReturnsNotFound()
{
    await _client.GetAndEnsureNotFoundAsync(
        "/api/doctors/9999",
        _outputHelper);
}
```

### GetAndEnsureBadRequestAsync

Get endpoint and assert 400 is returned:

```csharp
[Fact]
public async Task GetDoctors_MissingPageParam_ReturnsBadRequest()
{
    await _client.GetAndEnsureBadRequestAsync(
        "/api/doctors?page",
        _outputHelper);
}
```

### GetAndEnsureUnauthorizedAsync

Get endpoint and assert 401 is returned:

```csharp
[Fact]
public async Task GetProtectedResource_NoAuth_ReturnsUnauthorized()
{
    await _client.GetAndEnsureUnauthorizedAsync(
        "/api/admin/doctors",
        _outputHelper);
}
```

### GetAndEnsureForbiddenAsync

Get endpoint and assert 403 is returned:

```csharp
[Fact]
public async Task GetAdminResource_UserRole_ReturnsForbidden()
{
    await _client.GetAndEnsureForbiddenAsync(
        "/api/admin/settings",
        _outputHelper);
}
```

### GetAndEnsureRedirectAsync

Get endpoint and assert 302 redirect to expected location:

```csharp
[Fact]
public async Task GetOldUrl_RedirectsToNewUrl()
{
    // Disable auto-redirect
    var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    await client.GetAndEnsureRedirectAsync(
        "/old-doctors",
        "/api/doctors",
        _outputHelper);
}
```

## POST Methods

### PostAndDeserializeAsync<TRequest, TResponse>

Post data and deserialize response:

```csharp
[Fact]
public async Task CreateDoctor_ValidData_ReturnsCreatedDoctor()
{
    var request = new CreateDoctorRequest
    {
        Name = "Dr. Johnson",
        Specialty = "Pediatrics",
        LicenseNumber = "12345"
    };

    var result = await _client.PostAndDeserializeAsync<CreateDoctorRequest, DoctorDto>(
        "/api/doctors",
        request,
        _outputHelper);

    Assert.NotEqual(0, result.Id);
    Assert.Equal("Dr. Johnson", result.Name);
}
```

### PostAndEnsureNotFoundAsync

Post data and assert 404 is returned:

```csharp
[Fact]
public async Task AssignDoctorToPatient_InvalidPatientId_ReturnsNotFound()
{
    var request = new AssignDoctorRequest { DoctorId = 1 };

    await _client.PostAndEnsureNotFoundAsync(
        "/api/patients/9999/assign-doctor",
        request,
        _outputHelper);
}
```

### PostAndEnsureBadRequestAsync

Post data and assert 400 is returned:

```csharp
[Fact]
public async Task CreateDoctor_InvalidData_ReturnsBadRequest()
{
    var request = new CreateDoctorRequest
    {
        Name = "", // Invalid - empty name
        Specialty = "Cardiology"
    };

    await _client.PostAndEnsureBadRequestAsync(
        "/api/doctors",
        request,
        _outputHelper);
}
```

### PostAndEnsureUnauthorizedAsync

Post data and assert 401 is returned:

```csharp
[Fact]
public async Task CreateDoctor_NoAuth_ReturnsUnauthorized()
{
    var request = new CreateDoctorRequest
    {
        Name = "Dr. Test",
        Specialty = "Surgery"
    };

    await _client.PostAndEnsureUnauthorizedAsync(
        "/api/doctors",
        request,
        _outputHelper);
}
```

## PUT Methods

### PutAndDeserializeAsync<TRequest, TResponse>

Put data and deserialize response:

```csharp
[Fact]
public async Task UpdateDoctor_ValidData_ReturnsUpdatedDoctor()
{
    var request = new UpdateDoctorRequest
    {
        Name = "Dr. Smith Updated",
        Specialty = "Cardiology"
    };

    var result = await _client.PutAndDeserializeAsync<UpdateDoctorRequest, DoctorDto>(
        "/api/doctors/1",
        request,
        _outputHelper);

    Assert.Equal("Dr. Smith Updated", result.Name);
}
```

### PutAndEnsureNotFoundAsync

Put data and assert 404 is returned:

```csharp
[Fact]
public async Task UpdateDoctor_InvalidId_ReturnsNotFound()
{
    var request = new UpdateDoctorRequest
    {
        Name = "Dr. Test",
        Specialty = "Surgery"
    };

    await _client.PutAndEnsureNotFoundAsync(
        "/api/doctors/9999",
        request,
        _outputHelper);
}
```

### PutAndEnsureBadRequestAsync

Put data and assert 400 is returned:

```csharp
[Fact]
public async Task UpdateDoctor_InvalidData_ReturnsBadRequest()
{
    var request = new UpdateDoctorRequest
    {
        Name = "", // Invalid
        Specialty = "Cardiology"
    };

    await _client.PutAndEnsureBadRequestAsync(
        "/api/doctors/1",
        request,
        _outputHelper);
}
```

## PATCH Methods

### PatchAndDeserializeAsync<TRequest, TResponse>

Patch data and deserialize response:

```csharp
[Fact]
public async Task PatchDoctor_UpdateSpecialty_ReturnsUpdatedDoctor()
{
    var request = new JsonPatchDocument<DoctorDto>();
    request.Replace(d => d.Specialty, "Neurology");

    var result = await _client.PatchAndDeserializeAsync<JsonPatchDocument<DoctorDto>, DoctorDto>(
        "/api/doctors/1",
        request,
        _outputHelper);

    Assert.Equal("Neurology", result.Specialty);
}
```

## DELETE Methods

### DeleteAndEnsureSuccessAsync

Delete and assert 200/204 success:

```csharp
[Fact]
public async Task DeleteDoctor_ExistingId_ReturnsSuccess()
{
    await _client.DeleteAndEnsureSuccessAsync(
        "/api/doctors/1",
        _outputHelper);
}
```

### DeleteAndEnsureNotFoundAsync

Delete and assert 404 is returned:

```csharp
[Fact]
public async Task DeleteDoctor_InvalidId_ReturnsNotFound()
{
    await _client.DeleteAndEnsureNotFoundAsync(
        "/api/doctors/9999",
        _outputHelper);
}
```

## HttpResponseMessage Extensions

### EnsureNotFound

Assert response is 404:

```csharp
[Fact]
public async Task GetDoctor_InvalidId_ReturnsNotFound()
{
    var response = await _client.GetAsync("/api/doctors/9999");
    
    response.EnsureNotFound(); // Extension method
}
```

### EnsureBadRequest

Assert response is 400:

```csharp
[Fact]
public async Task CreateDoctor_InvalidData_ReturnsBadRequest()
{
    var response = await _client.PostAsJsonAsync("/api/doctors", new { });
    
    response.EnsureBadRequest();
}
```

### EnsureUnauthorized

Assert response is 401:

```csharp
[Fact]
public async Task GetProtected_NoAuth_ReturnsUnauthorized()
{
    var response = await _client.GetAsync("/api/admin");
    
    response.EnsureUnauthorized();
}
```

### EnsureForbidden

Assert response is 403:

```csharp
[Fact]
public async Task GetAdmin_UserRole_ReturnsForbidden()
{
    var response = await _client.GetAsync("/api/admin/settings");
    
    response.EnsureForbidden();
}
```

## StringContent Helpers

### StringContentFromObject

Create StringContent from object:

```csharp
[Fact]
public async Task CreateDoctor_UsingStringContent()
{
    var request = new CreateDoctorRequest { Name = "Dr. Test" };
    var content = HttpClientTestExtensionsHelpers.StringContentFromObject(request);

    var response = await _client.PostAsync("/api/doctors", content);
    
    response.EnsureSuccessStatusCode();
}
```

## Complete Test Examples

### CRUD Test Suite

```csharp
public class DoctorEndpointsTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _outputHelper;

    public DoctorEndpointsTests(
        CustomWebApplicationFactory<Program> factory,
        ITestOutputHelper outputHelper)
    {
        _client = factory.CreateClient();
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task GetAll_Returns3Doctors()
    {
        var result = await _client.GetAndDeserializeAsync<ListDoctorResponse>(
            "/api/doctors",
            _outputHelper);

        Assert.Equal(3, result.Doctors.Count);
    }

    [Fact]
    public async Task GetById_ExistingDoctor_ReturnsDoctor()
    {
        var result = await _client.GetAndDeserializeAsync<DoctorDto>(
            "/api/doctors/1",
            _outputHelper);

        Assert.Equal("Dr. Smith", result.Name);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        await _client.GetAndEnsureNotFoundAsync(
            "/api/doctors/9999",
            _outputHelper);
    }

    [Fact]
    public async Task Create_ValidDoctor_ReturnsCreated()
    {
        var request = new CreateDoctorRequest
        {
            Name = "Dr. Johnson",
            Specialty = "Pediatrics"
        };

        var result = await _client.PostAndDeserializeAsync<CreateDoctorRequest, DoctorDto>(
            "/api/doctors",
            request,
            _outputHelper);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Dr. Johnson", result.Name);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsBadRequest()
    {
        var request = new CreateDoctorRequest { Name = "" };

        await _client.PostAndEnsureBadRequestAsync(
            "/api/doctors",
            request,
            _outputHelper);
    }

    [Fact]
    public async Task Update_ValidData_ReturnsUpdated()
    {
        var request = new UpdateDoctorRequest
        {
            Name = "Dr. Smith Updated",
            Specialty = "Cardiology"
        };

        var result = await _client.PutAndDeserializeAsync<UpdateDoctorRequest, DoctorDto>(
            "/api/doctors/1",
            request,
            _outputHelper);

        Assert.Equal("Dr. Smith Updated", result.Name);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsSuccess()
    {
        await _client.DeleteAndEnsureSuccessAsync(
            "/api/doctors/1",
            _outputHelper);
    }
}
```

## Best Practices

### 1. Always Use ITestOutputHelper

```csharp
// ✅ Good - includes logging
var result = await _client.GetAndDeserializeAsync<DoctorDto>(
    "/api/doctors/1",
    _outputHelper);

// ❌ Bad - no logging when test fails
var result = await _client.GetAndDeserializeAsync<DoctorDto>(
    "/api/doctors/1");
```

### 2. One Test Class Per Endpoint

```csharp
// ✅ Good - focused test classes
DoctorEndpointsTests.cs
PatientEndpointsTests.cs
AppointmentEndpointsTests.cs

// ❌ Bad - mixed concerns
ApiTests.cs (all endpoints together)
```

### 3. Descriptive Test Names

```csharp
// ✅ Good - clear intent
[Fact]
public async Task GetDoctor_InvalidId_ReturnsNotFound()

// ❌ Bad - vague
[Fact]
public async Task Test1()
```

### 4. Use Theory for Multiple Scenarios

```csharp
[Theory]
[InlineData(1, "Dr. Smith")]
[InlineData(2, "Dr. Jones")]
[InlineData(3, "Dr. Williams")]
public async Task GetDoctor_ValidIds_ReturnsDoctors(int id, string expectedName)
{
    var result = await _client.GetAndDeserializeAsync<DoctorDto>(
        $"/api/doctors/{id}",
        _outputHelper);

    Assert.Equal(expectedName, result.Name);
}
```

### 5. Test Error Scenarios

```csharp
[Fact]
public async Task GetDoctor_InvalidId_ReturnsNotFound()
{
    await _client.GetAndEnsureNotFoundAsync("/api/doctors/9999", _outputHelper);
}

[Fact]
public async Task CreateDoctor_MissingName_ReturnsBadRequest()
{
    await _client.PostAndEnsureBadRequestAsync(
        "/api/doctors",
        new CreateDoctorRequest { Specialty = "Cardiology" },
        _outputHelper);
}
```

## Common Patterns

### Authentication Testing

```csharp
[Fact]
public async Task GetProtectedResource_WithAuth_ReturnsData()
{
    // Add auth header
    _client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", _testToken);

    var result = await _client.GetAndDeserializeAsync<SecureData>(
        "/api/secure/data",
        _outputHelper);

    Assert.NotNull(result);
}

[Fact]
public async Task GetProtectedResource_NoAuth_ReturnsUnauthorized()
{
    await _client.GetAndEnsureUnauthorizedAsync(
        "/api/secure/data",
        _outputHelper);
}
```

### Pagination Testing

```csharp
[Fact]
public async Task GetDoctors_Page1_Returns10Doctors()
{
    var result = await _client.GetAndDeserializeAsync<PagedResponse<DoctorDto>>(
        "/api/doctors?page=1&pageSize=10",
        _outputHelper);

    Assert.Equal(10, result.Items.Count);
    Assert.Equal(1, result.PageNumber);
}
```

### Search Testing

```csharp
[Fact]
public async Task SearchDoctors_BySpecialty_ReturnsMatches()
{
    var result = await _client.GetAndDeserializeAsync<List<DoctorDto>>(
        "/api/doctors?specialty=Cardiology",
        _outputHelper);

    Assert.All(result, d => Assert.Equal("Cardiology", d.Specialty));
}
```

## References

- GitHub: https://github.com/ardalis/HttpClientTestExtensions
- NuGet: https://www.nuget.org/packages/Ardalis.HttpClientTestExtensions
- Blog: https://ardalis.com/keep-tests-short-and-dry-with-extensions/
- NimblePros Blog: https://blog.nimblepros.com/blogs/aspnetcore-integration-test-helpers/
- Latest Version: 4.2.0
- License: MIT
