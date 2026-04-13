using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;
using Api.UseCases.Recipes.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class ListRecipesRequest
{
  public string? Search { get; set; }
  public RecipeType? Type { get; set; }
  public RecipeCategory? Category { get; set; }
}

public class ListRecipes(IMediator mediator) : Endpoint<ListRecipesRequest, List<RecipeDto>>
{
  public override void Configure()
  {
    Get("/api/admin/recipes");
    Policies("recipe.read");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(ListRecipesRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ListRecipesQuery(req.Search, req.Type, req.Category), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class ListRecipesSummary : Summary<ListRecipes>
{
  public ListRecipesSummary()
  {
    Summary = "List all recipes";
    Description = "Returns all recipes, optionally filtered by search term, type, or category. Requires recipe.read permission.";
    Response(200, "List of recipes.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.read).");
  }
}
