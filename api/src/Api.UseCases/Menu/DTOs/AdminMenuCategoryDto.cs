namespace Api.UseCases.Menu.DTOs;

/// <summary>
///   DTO danh mục cho admin menu — bao gồm cả inactive categories.
/// </summary>
public record AdminMenuCategoryDto(
  int Id,
  string Name,
  string? Description,
  bool IsActive,
  List<AdminMenuProductDto> Products
);
