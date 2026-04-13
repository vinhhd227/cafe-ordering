using Api.Core.Aggregates.RecipeAggregate;
using Api.Core.Aggregates.RecipeAggregate.Specifications;
using Api.UseCases.Interfaces;

namespace Api.UseCases.Recipes.Chat;

public class ChatRecipeHandler(
  IReadRepositoryBase<Recipe> repository,
  IGeminiService geminiService)
  : IQueryHandler<ChatRecipeQuery, Result<string>>
{
  public async ValueTask<Result<string>> Handle(ChatRecipeQuery request, CancellationToken ct)
  {
    var spec = new RecipeByIdSpec(request.RecipeId);
    var recipe = await repository.FirstOrDefaultAsync(spec, ct);

    if (recipe is null)
      return Result.NotFound($"Recipe {request.RecipeId} not found.");

    var systemPrompt = $"""
      You are a knowledgeable barista assistant for a Vietnamese café called 5AM Coffee.
      Answer questions about the following recipe concisely and helpfully in the same language the user writes in.
      If asked about something unrelated to this recipe or café operations, politely redirect to the recipe topic.

      Recipe: {recipe.Name}
      Type: {recipe.Type}
      Category: {recipe.Category}
      Yield: {recipe.Yield ?? "Not specified"}

      Recipe Content:
      {recipe.Content}

      {(recipe.Notes is not null ? $"Notes:\n{recipe.Notes}" : "")}
      """;

    var history = request.History
      .Select(h => new ChatTurn(h.Role, h.Text))
      .ToList();

    try
    {
      var reply = await geminiService.ChatAsync(systemPrompt, history, request.Message, ct);
      return Result.Success(reply);
    }
    catch (Exception ex)
    {
      return Result.Error($"AI service error: {ex.Message}");
    }
  }
}
