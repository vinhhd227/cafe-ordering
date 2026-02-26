namespace Api.UseCases.Categories.DTOs;

/// <summary>
///   DTO chi tiết cho Category
/// </summary>
public record CategoryDto(
  int Id,
  string Name,
  string? Description,
  bool IsActive,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
