using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.Create;
using Api.UseCases.Recipes.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class CreateRecipeRequest
{
  public string Name { get; set; } = string.Empty;
  public RecipeType Type { get; set; }
  public RecipeCategory Category { get; set; }
  public string Content { get; set; } = string.Empty;
  public string? Yield { get; set; }
  public string? Notes { get; set; }
}

public class CreateRecipe(IMediator mediator) : Endpoint<CreateRecipeRequest, RecipeDto>
{
  public override void Configure()
  {
    Post("/api/admin/recipes");
    Policies("recipe.create");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(CreateRecipeRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreateRecipeCommand(
      req.Name,
      req.Type,
      req.Category,
      req.Content,
      req.Yield,
      req.Notes), ct);

    await this.SendResultAsync(result, ct);
  }
}

public class CreateRecipeSummary : Summary<CreateRecipe>
{
  public CreateRecipeSummary()
  {
    Summary = "Create a new recipe";
    Description = "Creates a new recipe or preparation formula. Requires recipe.create permission.";
    Response(200, "Recipe created successfully.");
    Response(400, "Validation failed — name or content is empty.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.create).");
  }
}
