using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.RecipeAggregate.Specifications;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Update;

public class UpdateRecipeHandler(IRepositoryBase<Recipe> repository)
  : ICommandHandler<UpdateRecipeCommand, Result<RecipeDto>>
{
  public async ValueTask<Result<RecipeDto>> Handle(UpdateRecipeCommand request, CancellationToken ct)
  {
    var spec = new RecipeByIdSpec(request.RecipeId);
    var recipe = await repository.FirstOrDefaultAsync(spec, ct);

    if (recipe is null)
      return Result.NotFound($"Recipe {request.RecipeId} not found.");

    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Recipe name is required."));

    if (string.IsNullOrWhiteSpace(request.Content))
      return Result.Invalid(new ValidationError("Content", "Recipe content is required."));

    recipe.Update(
      request.Name,
      request.Type,
      request.Category,
      request.Content,
      request.Yield,
      request.Notes);

    await repository.UpdateAsync(recipe, ct);

    return Result.Success(new RecipeDto(
      recipe.Id,
      recipe.Name,
      recipe.Type.ToString(),
      recipe.Category.ToString(),
      recipe.Content,
      recipe.Yield,
      recipe.Notes,
      recipe.IsActive,
      recipe.CreatedAt,
      recipe.UpdatedAt
    ));
  }
}
