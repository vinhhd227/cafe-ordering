using Api.Infrastructure.Identity;
using Testcontainers.PostgreSql;

namespace Api.IntegrationTests.Fixtures;

/// <summary>
/// Shared PostgreSQL container fixture — one container instance per test collection.
/// Runs migrations for both business (AppDbContext) and identity (AppIdentityDbContext).
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
  private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
    .WithDatabase("cafe_test")
    .WithUsername("postgres")
    .WithPassword("test_password")
    .Build();

  public AppDbContext BusinessDb { get; private set; } = default!;
  public AppIdentityDbContext IdentityDb { get; private set; } = default!;
  public string ConnectionString => _postgres.GetConnectionString();

  public async Task InitializeAsync()
  {
    await _postgres.StartAsync();

    BusinessDb = BuildBusinessDb();
    IdentityDb = BuildIdentityDb();

    await BusinessDb.Database.MigrateAsync();
    await IdentityDb.Database.MigrateAsync();
  }

  public AppDbContext BuildBusinessDb() =>
    new(new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql(ConnectionString, o =>
        o.MigrationsHistoryTable("__EFMigrationsHistory", "business"))
      .Options);

  public async Task DisposeAsync()
  {
    await BusinessDb.DisposeAsync();
    await IdentityDb.DisposeAsync();
    await _postgres.DisposeAsync();
  }

  private AppIdentityDbContext BuildIdentityDb() =>
    new(new DbContextOptionsBuilder<AppIdentityDbContext>()
      .UseNpgsql(ConnectionString, o =>
        o.MigrationsHistoryTable("__EFMigrationsHistory", "identity"))
      .Options);
}

[CollectionDefinition(nameof(DatabaseCollection))]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
