using Api.FunctionalTests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Api.FunctionalTests.Fixtures;

/// <summary>
/// WebApplicationFactory that spins up the full API against a real PostgreSQL container.
/// Migrations and seed data run automatically via Database:ApplyMigrationsOnStartup.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
  private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
    .WithDatabase("cafe_functional_test")
    .WithUsername("postgres")
    .WithPassword("test_password")
    .Build();

  public async Task InitializeAsync()
  {
    await _postgres.StartAsync();

    // Force the WebApplication to build so migrations run now (not lazily on first request)
    _ = Server;
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.UseEnvironment("Testing");

    builder.ConfigureAppConfiguration((_, config) =>
    {
      config.AddInMemoryCollection(new Dictionary<string, string?>
      {
        // Point both DbContexts to the test container
        ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),

        // JWT — must match JwtTokenHelper constants
        ["Jwt:Key"]      = JwtTokenHelper.TestKey,
        ["Jwt:Issuer"]   = JwtTokenHelper.TestIssuer,
        ["Jwt:Audience"] = JwtTokenHelper.TestAudience,

        // Trigger migrations + seeding on startup
        ["Database:ApplyMigrationsOnStartup"] = "true",

        // Seed admin account
        ["AdminAccount:Username"] = "admin",
        ["AdminAccount:Password"] = "Admin@123456",
        ["AdminAccount:FullName"] = "Test Admin"
      });
    });
  }

  /// <summary>Creates an HttpClient with the Admin JWT pre-attached.</summary>
  public HttpClient CreateAdminClient()
  {
    var client = CreateClient();
    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", JwtTokenHelper.ForAdmin());
    return client;
  }

  /// <summary>Creates an HttpClient with a Staff JWT pre-attached.</summary>
  public HttpClient CreateStaffClient(string[]? permissions = null)
  {
    var client = CreateClient();
    var token = permissions is null
      ? JwtTokenHelper.ForStaff()
      : JwtTokenHelper.ForStaffWithPermissions(permissions);
    client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  public new async Task DisposeAsync()
  {
    await base.DisposeAsync();
    await _postgres.DisposeAsync();
  }
}

[CollectionDefinition(nameof(FunctionalTestCollection))]
public class FunctionalTestCollection : ICollectionFixture<ApiFactory> { }
