using Api.UseCases.Recipes.Chat;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Recipes;

public sealed class ChatRecipeHttpRequest
{
  public int RecipeId { get; set; }
  public List<ChatMessageItem> History { get; set; } = [];
  public string Message { get; set; } = string.Empty;
}

public sealed class ChatRecipeHttpResponse
{
  public string Reply { get; set; } = string.Empty;
}

public class ChatRecipe(IMediator mediator) : Endpoint<ChatRecipeHttpRequest, ChatRecipeHttpResponse>
{
  public override void Configure()
  {
    Post("/api/admin/recipes/{RecipeId}/chat");
    Policies("recipe.read");
    DontAutoTag();
    Description(b => b.WithTags("Recipes"));
  }

  public override async Task HandleAsync(ChatRecipeHttpRequest req, CancellationToken ct)
  {
    var query = new ChatRecipeQuery(req.RecipeId, req.History, req.Message);
    var result = await mediator.Send(query, ct);

    if (result.IsSuccess)
      await SendAsync(new ChatRecipeHttpResponse { Reply = result.Value }, cancellation: ct);
    else
      await this.SendResultAsync(result, ct);
  }
}

public class ChatRecipeSummary : Summary<ChatRecipe>
{
  public ChatRecipeSummary()
  {
    Summary = "Chat with AI about a recipe";
    Description = "Sends a message to Gemini AI with the recipe as context and returns the AI reply. Requires recipe.read permission.";
    Response<ChatRecipeHttpResponse>(200, "AI reply.");
    Response(401, "Authentication required.");
    Response(403, "Insufficient permissions (requires recipe.read).");
    Response(404, "Recipe not found.");
    Response(500, "AI service unavailable.");
  }
}
