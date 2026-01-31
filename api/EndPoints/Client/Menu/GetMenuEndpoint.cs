using api.Contracts.Menu;
using api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace api.EndPoints.Client.Menu;

public static class GetMenuEndpoint
{
    public static IEndpointRouteBuilder MapGetMenu(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/menu", async (AppDbContext db) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.Name)
                .Select(category => new GetMenu.CategoryDto(
                    category.Id,
                    category.Name,
                    category.Products
                        .Where(product => product.IsActive)
                        .OrderBy(product => product.Name)
                        .Select(product => new GetMenu.ProductDto(
                            product.Id,
                            product.Name,
                            product.Description,
                            product.Price,
                            product.ImageUrl,
                            product.HasTemperatureOption,
                            product.HasIceLevelOption,
                            product.HasSugarLevelOption
                        ))
                        .ToList()
                ))
                .ToListAsync();

            return Results.Ok(new GetMenu.Response(categories));
        });

        return app;
    }
}
