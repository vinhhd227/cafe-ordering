namespace Api.UseCases.Interfaces;

public record ChatTurn(string Role, string Text);

public interface IGeminiService
{
  Task<string> ChatAsync(
    string systemPrompt,
    IReadOnlyList<ChatTurn> history,
    string userMessage,
    CancellationToken ct = default);
}
