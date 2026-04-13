using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Create;

public record CreateRecipeCommand(
  string Name,
  RecipeType Type,
  RecipeCategory Category,
  string Content,
  string? Yield,
  string? Notes
) : ICommand<Result<RecipeDto>>;
