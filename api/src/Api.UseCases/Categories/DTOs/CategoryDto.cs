namespace Api.UseCases.Categories.DTOs;

/// <summary>
///   DTO chi tiết cho Category
/// </summary>
public record CategoryDto(
  int Id,
  string Name,
  string? Description,
  string? ImageUrl,
  int SortOrder,
  bool IsActive,
  int ProductCount,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
