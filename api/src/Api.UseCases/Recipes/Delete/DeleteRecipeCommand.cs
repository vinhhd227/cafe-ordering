namespace Api.UseCases.Recipes.Delete;

public record DeleteRecipeCommand(int RecipeId, string DeletedBy) : ICommand<Result>;
