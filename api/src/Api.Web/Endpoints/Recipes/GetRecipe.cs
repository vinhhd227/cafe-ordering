using Api.UseCases.Recipes.DTOs;
using Api.UseCases.Recipes.Get;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class GetRecipeRequest
{
  public int RecipeId { get; set; }
}

public class GetRecipe(IMediator mediator) : Endpoint<GetRecipeRequest, RecipeDto>
{
  public override void Configure()
  {
    Get("/api/admin/recipes/{RecipeId}");
    Policies("recipe.read");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(GetRecipeRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetRecipeQuery(req.RecipeId), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class GetRecipeSummary : Summary<GetRecipe>
{
  public GetRecipeSummary()
  {
    Summary = "Get a recipe by ID";
    Description = "Returns a single recipe by its ID. Requires recipe.read permission.";
    Response(200, "Recipe details.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.read).");
    Response(404, "Recipe not found.");
  }
}
