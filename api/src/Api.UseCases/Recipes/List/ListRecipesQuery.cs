using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.List;

public record ListRecipesQuery(
  string? Search = null,
  RecipeType? Type = null,
  RecipeCategory? Category = null
) : IQuery<Result<List<RecipeDto>>>;
