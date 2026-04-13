using System.Security.Claims;
using Api.UseCases.Recipes.Delete;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class DeleteRecipeRequest
{
  public int RecipeId { get; set; }
}

public class DeleteRecipe(IMediator mediator) : Ep.Req<DeleteRecipeRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/recipes/{RecipeId}");
    Policies("recipe.delete");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(DeleteRecipeRequest req, CancellationToken ct)
  {
    var deletedBy = User.FindFirstValue(ClaimTypes.Name) ?? "system";
    var result = await mediator.Send(new DeleteRecipeCommand(req.RecipeId, deletedBy), ct);
    await this.SendResultAsync(result, ct);
  }
}

public class DeleteRecipeSummary : Summary<DeleteRecipe>
{
  public DeleteRecipeSummary()
  {
    Summary = "Delete a recipe";
    Description = "Soft-deletes a recipe. Requires recipe.delete permission.";
    Response(204, "Recipe deleted successfully.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.delete).");
    Response(404, "Recipe not found.");
  }
}
