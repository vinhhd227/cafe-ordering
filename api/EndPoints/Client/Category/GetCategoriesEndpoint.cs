using api.Contracts.Categories;
using api.Infrastructure.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace api.EndPoints.Client.Category;

public static class GetCategoriesEndpoint
{
    public static IEndpointRouteBuilder MapGetCategories(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/categories", async (AppDbContext db, IMapper mapper) =>
        {
            var categories = await db.Categories
                .AsNoTracking()
                .Where(category => category.IsActive)
                .OrderBy(category => category.Name)
                .ToListAsync();

            var categoryDtos = mapper.Map<List<GetCategories.CategoryDto>>(categories);
            return Results.Ok(new GetCategories.Response(categoryDtos));
        });

        return app;
    }
}
