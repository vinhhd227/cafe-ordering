using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.RecipeAggregate.Specifications;

namespace Api.UseCases.Recipes.Delete;

public class DeleteRecipeHandler(IRepositoryBase<Recipe> repository)
  : ICommandHandler<DeleteRecipeCommand, Result>
{
  public async ValueTask<Result> Handle(DeleteRecipeCommand request, CancellationToken ct)
  {
    var spec = new RecipeByIdSpec(request.RecipeId);
    var recipe = await repository.FirstOrDefaultAsync(spec, ct);

    if (recipe is null)
      return Result.NotFound($"Recipe {request.RecipeId} not found.");

    recipe.Delete(request.DeletedBy);
    await repository.UpdateAsync(recipe, ct);

    return Result.Success();
  }
}
