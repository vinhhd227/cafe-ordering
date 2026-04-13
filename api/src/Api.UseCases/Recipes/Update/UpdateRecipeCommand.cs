using Api.Core.Aggregates.RecipeAggregate;
using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Update;

public record UpdateRecipeCommand(
  int RecipeId,
  string Name,
  RecipeType Type,
  RecipeCategory Category,
  string Content,
  string? Yield,
  string? Notes
) : ICommand<Result<RecipeDto>>;
