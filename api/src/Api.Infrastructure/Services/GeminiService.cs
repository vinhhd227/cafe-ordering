using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Api.UseCases.Interfaces;

namespace Api.Infrastructure.Services;

public class GeminiOptions
{
  public string ApiKey { get; set; } = string.Empty;
  public string Model { get; set; } = "gemini-2.0-flash";
}

public class GeminiService(
  HttpClient http,
  IOptions<GeminiOptions> options,
  ILogger<GeminiService> logger)
  : IGeminiService
{
  private readonly GeminiOptions _opts = options.Value;

  public async Task<string> ChatAsync(
    string systemPrompt,
    IReadOnlyList<ChatTurn> history,
    string userMessage,
    CancellationToken ct = default)
  {
    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_opts.Model}:generateContent?key={_opts.ApiKey}";

    var contents = history
      .Select(h => new GeminiContent(h.Role, [new GeminiPart(h.Text)]))
      .Append(new GeminiContent("user", [new GeminiPart(userMessage)]))
      .ToList();

    var body = new GeminiRequest(
      new GeminiSystemInstruction([new GeminiPart(systemPrompt)]),
      contents);

    var response = await http.PostAsJsonAsync(url, body, ct);

    if (!response.IsSuccessStatusCode)
    {
      if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
      {
        logger.LogWarning("Gemini API rate limit reached");
        return "⚠️ Dịch vụ AI đang bận (rate limit), vui lòng thử lại sau ít giây.";
      }

      var errorBody = await response.Content.ReadAsStringAsync(ct);
      logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, errorBody);
      throw new HttpRequestException($"Gemini API returned {(int)response.StatusCode}");
    }

    var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);
    var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

    return text ?? "Không thể tạo phản hồi. Vui lòng thử lại.";
  }
}

// ── Gemini API request/response models ────────────────────────────────────────
file record GeminiPart(
  [property: JsonPropertyName("text")] string Text);

file record GeminiContent(
  [property: JsonPropertyName("role")] string Role,
  [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

file record GeminiSystemInstruction(
  [property: JsonPropertyName("parts")] List<GeminiPart> Parts);

file record GeminiRequest(
  [property: JsonPropertyName("system_instruction")] GeminiSystemInstruction SystemInstruction,
  [property: JsonPropertyName("contents")] List<GeminiContent> Contents);

file record GeminiResponsePart(
  [property: JsonPropertyName("text")] string? Text);

file record GeminiResponseContent(
  [property: JsonPropertyName("parts")] List<GeminiResponsePart>? Parts);

file record GeminiCandidate(
  [property: JsonPropertyName("content")] GeminiResponseContent? Content);

file record GeminiResponse(
  [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);
