# HttpClientTestExtensions Examples

Complete test suite examples.

## Full CRUD Test Suite

```csharp
using Ardalis.HttpClientTestExtensions;
using Xunit;
using Xunit.Abstractions;

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
    public async Task List_Returns3Doctors()
    {
        var result = await _client.GetAndDeserializeAsync<ListDoctorResponse>(
            "/api/doctors", _outputHelper);

        Assert.Equal(3, result.Doctors.Count);
        Assert.Contains(result.Doctors, x => x.Name == "Dr. Smith");
    }

    [Fact]
    public async Task Get_ExistingId_ReturnsDoctor()
    {
        var result = await _client.GetAndDeserializeAsync<DoctorDto>(
            "/api/doctors/1", _outputHelper);

        Assert.Equal(1, result.Id);
        Assert.Equal("Dr. Smith", result.Name);
    }

    [Fact]
    public async Task Get_InvalidId_ReturnsNotFound()
    {
        await _client.GetAndEnsureNotFoundAsync("/api/doctors/9999", _outputHelper);
    }

    [Fact]
    public async Task Create_ValidData_ReturnsCreated()
    {
        var request = new CreateDoctorRequest
        {
            Name = "Dr. Johnson",
            Specialty = "Pediatrics",
            LicenseNumber = "LIC123"
        };

        var result = await _client.PostAndDeserializeAsync<CreateDoctorRequest, DoctorDto>(
            "/api/doctors", request, _outputHelper);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Dr. Johnson", result.Name);
    }

    [Fact]
    public async Task Create_InvalidData_ReturnsBadRequest()
    {
        var request = new CreateDoctorRequest { Name = "" }; // Invalid

        await _client.PostAndEnsureBadRequestAsync(
            "/api/doctors", request, _outputHelper);
    }

    [Fact]
    public async Task Update_ExistingDoctor_ReturnsUpdated()
    {
        var request = new UpdateDoctorRequest
        {
            Name = "Dr. Smith Updated",
            Specialty = "Cardiology"
        };

        var result = await _client.PutAndDeserializeAsync<UpdateDoctorRequest, DoctorDto>(
            "/api/doctors/1", request, _outputHelper);

        Assert.Equal("Dr. Smith Updated", result.Name);
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        var request = new UpdateDoctorRequest { Name = "Test" };

        await _client.PutAndEnsureNotFoundAsync(
            "/api/doctors/9999", request, _outputHelper);
    }

    [Fact]
    public async Task Delete_ExistingId_ReturnsSuccess()
    {
        await _client.DeleteAndEnsureSuccessAsync("/api/doctors/3", _outputHelper);
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        await _client.DeleteAndEnsureNotFoundAsync("/api/doctors/9999", _outputHelper);
    }
}

// Custom Factory
public class CustomWebApplicationFactory<TProgram> 
    : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}
```

See SKILL.md for complete API reference.
