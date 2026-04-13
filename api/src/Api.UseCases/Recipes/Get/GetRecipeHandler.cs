using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.RecipeAggregate.Specifications;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Get;

public class GetRecipeHandler(IReadRepositoryBase<Recipe> repository)
  : IQueryHandler<GetRecipeQuery, Result<RecipeDto>>
{
  public async ValueTask<Result<RecipeDto>> Handle(GetRecipeQuery request, CancellationToken ct)
  {
    var spec = new RecipeByIdSpec(request.RecipeId);
    var recipe = await repository.FirstOrDefaultAsync(spec, ct);

    if (recipe is null)
      return Result.NotFound($"Recipe {request.RecipeId} not found.");

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
