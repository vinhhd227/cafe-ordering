namespace Api.UseCases.Categories.DTOs;

/// <summary>
///   DTO chi tiết cho Category
/// </summary>
public record CategoryDto(
  int Id,
  string Name,
  bool IsActive,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
