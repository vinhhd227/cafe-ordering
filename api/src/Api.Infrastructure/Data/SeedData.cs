using Api.Core.Aggregates.CategoryAggregate;

namespace Api.Infrastructure.Data;

public static class SeedData
{
  public static async Task InitializeAsync(AppDbContext context)
  {
    // Kiểm tra đã có data chưa
    if (context.Categories.Any())
    {
      return;
    }

    // Seed categories
    var categories = new[]
    {
      Category.Create("Cà phê"), Category.Create("Trà"), Category.Create("Nước ép"), Category.Create("Sinh tố"),
      Category.Create("Topping")
    };

    context.Categories.AddRange(categories);
    await context.SaveChangesAsync();
  }
}
