namespace Api.UseCases.Recipes.Chat;

public record ChatMessageItem(string Role, string Text);

public record ChatRecipeQuery(
  int RecipeId,
  IReadOnlyList<ChatMessageItem> History,
  string Message) : IQuery<Result<string>>;
