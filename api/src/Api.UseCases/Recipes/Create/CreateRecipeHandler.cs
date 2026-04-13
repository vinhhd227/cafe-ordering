using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Create;

public class CreateRecipeHandler(IRepositoryBase<Recipe> repository)
  : ICommandHandler<CreateRecipeCommand, Result<RecipeDto>>
{
  public async ValueTask<Result<RecipeDto>> Handle(CreateRecipeCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Recipe name is required."));

    if (string.IsNullOrWhiteSpace(request.Content))
      return Result.Invalid(new ValidationError("Content", "Recipe content is required."));

    var recipe = Recipe.Create(
      request.Name,
      request.Type,
      request.Category,
      request.Content,
      request.Yield,
      request.Notes);

    await repository.AddAsync(recipe, ct);

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
