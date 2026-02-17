namespace Api.UseCases.Products.DTOs;

/// <summary>
///   DTO tóm tắt cho danh sách Products
/// </summary>
public record ProductSummaryDto(
  int Id,
  string Name,
  decimal Price,
  bool IsActive,
  string? ImageUrl,
  string? CategoryName
);
