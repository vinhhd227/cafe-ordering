using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.RecipeAggregate.Specifications;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.List;

public class ListRecipesHandler(IReadRepositoryBase<Recipe> repository)
  : IQueryHandler<ListRecipesQuery, Result<List<RecipeDto>>>
{
  public async ValueTask<Result<List<RecipeDto>>> Handle(ListRecipesQuery request, CancellationToken ct)
  {
    var spec = new AllRecipesSpec(request.Search, request.Type, request.Category);
    var recipes = await repository.ListAsync(spec, ct);

    var dtos = recipes.Select(r => new RecipeDto(
      r.Id,
      r.Name,
      r.Type.ToString(),
      r.Category.ToString(),
      r.Content,
      r.Yield,
      r.Notes,
      r.IsActive,
      r.CreatedAt,
      r.UpdatedAt
    )).ToList();

    return Result.Success(dtos);
  }
}
