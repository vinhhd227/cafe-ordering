using Api.Core.Aggregates.CategoryAggregate;
using Microsoft.Extensions.Logging;

namespace Api.Infrastructure.Data;

/// <summary>
/// Seeds business data into AppDbContext.
/// Identity seeding (users/roles) is handled by IdentitySeedData in the Identity folder.
/// </summary>
public static class SeedData
{
  public static async Task InitializeAsync(AppDbContext context, ILogger? logger = null)
  {
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
  }
}
