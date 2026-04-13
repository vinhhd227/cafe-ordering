namespace Api.UseCases.Recipes.DTOs;

public record RecipeDto(
  int Id,
  string Name,
  string Type,
  string Category,
  string Content,
  string? Yield,
  string? Notes,
  bool IsActive,
  DateTime? CreatedAt,
  DateTime? UpdatedAt
);
