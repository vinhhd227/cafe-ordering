using Api.UseCases.Recipes.DTOs;

namespace Api.UseCases.Recipes.Get;

public record GetRecipeQuery(int RecipeId) : IQuery<Result<RecipeDto>>;
