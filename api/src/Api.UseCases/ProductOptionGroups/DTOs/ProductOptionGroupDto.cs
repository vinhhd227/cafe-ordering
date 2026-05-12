namespace Api.UseCases.ProductOptionGroups.DTOs;

public record ProductOptionValueDto(
  int Id,
  string Name,
  decimal Price,
  decimal? CostPrice,
  bool IsInStock,
  int DisplayOrder);

public record LinkedProductDto(
  int Id,
  string Name,
  decimal Price,
  string? ImageUrl);

public record ProductOptionGroupDto(
  int Id,
  string Name,
  bool IsRequired,
  bool AllowMultiple,
  bool AllowQuantity,
  bool IsActive,
  int DisplayOrder,
  List<ProductOptionValueDto> Values,
  List<LinkedProductDto> LinkedProducts);

public record ProductOptionGroupSummaryDto(
  int Id,
  string Name,
  bool IsRequired,
  bool AllowMultiple,
  bool AllowQuantity,
  bool IsActive,
  int ValueCount,
  int LinkedProductCount,
  List<string> ValueNames);
