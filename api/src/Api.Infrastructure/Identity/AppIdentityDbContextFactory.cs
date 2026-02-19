using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Used by EF CLI tools at design time (migrations, scaffolding).
/// Bypasses the full DI container.
/// </summary>
public class AppIdentityDbContextFactory : IDesignTimeDbContextFactory<AppIdentityDbContext>
{
  public AppIdentityDbContext CreateDbContext(string[] args)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json", optional: false)
      .AddJsonFile("appsettings.Development.json", optional: true)
      .AddEnvironmentVariables()
      .Build();

    var connectionString = configuration.GetConnectionString("DefaultConnection");

    var optionsBuilder = new DbContextOptionsBuilder<AppIdentityDbContext>();
    optionsBuilder.UseNpgsql(connectionString, o =>
      o.MigrationsHistoryTable("__EFMigrationsHistory", "identity"));

    return new AppIdentityDbContext(optionsBuilder.Options);
  }
}
