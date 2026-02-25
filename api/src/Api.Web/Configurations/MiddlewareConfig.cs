using Api.Infrastructure.Data;
using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Scalar.AspNetCore;

namespace Api.Web.Configurations;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
    }
    else
    {
      app.UseDefaultExceptionHandler();
      app.UseHsts();
    }

    app.UseFastEndpoints();

    if (app.Environment.IsDevelopment())
    {
      app.UseSwaggerGen(options =>
      {
        options.Path = "/openapi/{documentName}.json";
      });
      app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    var shouldMigrate = app.Environment.IsDevelopment() ||
                        app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

    if (shouldMigrate)
    {
      await MigrateDatabasesAsync(app);
      await SeedDatabaseAsync(app);
    }

    return app;
  }

  private static async Task MigrateDatabasesAsync(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      logger.LogInformation("Applying business DB migrations...");
      var context = services.GetRequiredService<AppDbContext>();
      await context.Database.MigrateAsync();

      logger.LogInformation("Applying identity DB migrations...");
      var identityContext = services.GetRequiredService<AppIdentityDbContext>();
      await identityContext.Database.MigrateAsync();

      logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred migrating the DB. {exceptionMessage}", ex.Message);
      throw;
    }
  }

  private static async Task SeedDatabaseAsync(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
      logger.LogInformation("Seeding database...");

      // Seed business data
      var context = services.GetRequiredService<AppDbContext>();
      await SeedData.InitializeAsync(context, logger);

      // Seed identity data (roles, admin user)
      var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
      var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
      var config = services.GetRequiredService<IConfiguration>();
      await IdentitySeedData.SeedAsync(userManager, roleManager, config, logger);

      logger.LogInformation("Database seeded successfully");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
  }
}
