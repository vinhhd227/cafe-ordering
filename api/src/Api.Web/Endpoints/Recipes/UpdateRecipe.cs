using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;
using Api.UseCases.Recipes.Update;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class UpdateRecipeRequest
{
  public int RecipeId { get; set; }
  public string Name { get; set; } = string.Empty;
  public RecipeType Type { get; set; }
  public RecipeCategory Category { get; set; }
  public string Content { get; set; } = string.Empty;
  public string? Yield { get; set; }
  public string? Notes { get; set; }
}

public class UpdateRecipe(IMediator mediator) : Endpoint<UpdateRecipeRequest, RecipeDto>
{
  public override void Configure()
  {
    Put("/api/admin/recipes/{RecipeId}");
    Policies("recipe.update");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(UpdateRecipeRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new UpdateRecipeCommand(
      req.RecipeId,
      req.Name,
      req.Type,
      req.Category,
      req.Content,
      req.Yield,
      req.Notes), ct);

    await this.SendResultAsync(result, ct);
  }
}

public class UpdateRecipeSummary : Summary<UpdateRecipe>
{
  public UpdateRecipeSummary()
  {
    Summary = "Update a recipe";
    Description = "Updates an existing recipe. Requires recipe.update permission.";
    Response(200, "Recipe updated successfully.");
    Response(400, "Validation failed.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.update).");
    Response(404, "Recipe not found.");
  }
}
