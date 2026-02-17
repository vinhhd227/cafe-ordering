namespace Api.UseCases.Products.DTOs;

/// <summary>
///   DTO chi tiết cho single Product
/// </summary>
public record ProductDto(
  int Id,
  int CategoryId,
  string? CategoryName,
  string Name,
  string? Description,
  decimal Price,
  bool IsActive,
  string? ImageUrl,
  bool HasTemperatureOption,
  bool HasIceLevelOption,
  bool HasSugarLevelOption,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
