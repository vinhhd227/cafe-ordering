using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Data;

public static class SeedData
{
  public static async Task InitializeAsync(
    AppDbContext context,
    UserManager<ApplicationUser>? userManager = null,
    RoleManager<ApplicationRole>? roleManager = null,
    ILogger? logger = null)
  {
    // Seed categories
    if (!context.Categories.Any())
    {
      var categories = new[]
      {
        Category.Create("Cà phê"),
        Category.Create("Trà"),
        Category.Create("Nước ép"),
        Category.Create("Sinh tố"),
        Category.Create("Topping")
      };

      context.Categories.AddRange(categories);
      await context.SaveChangesAsync();

      logger?.LogInformation("Seeded {Count} categories", categories.Length);
    }

    // Seed Identity (roles and admin user)
    if (userManager != null && roleManager != null && logger != null)
    {
      await IdentitySeedData.SeedIdentityAsync(userManager, roleManager, logger);
    }
  }
}
