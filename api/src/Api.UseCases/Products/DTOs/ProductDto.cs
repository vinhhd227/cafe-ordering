namespace Api.UseCases.Products.DTOs;

public record ProductVariantValueDto(
  int Id,
  string Label,
  decimal Price,
  bool IsDefault,
  int DisplayOrder);

public record ProductVariantGroupDto(
  int Id,
  string Name,
  bool IsRequired,
  string SelectionType,
  int DisplayOrder,
  List<ProductVariantValueDto> Values);

public record ProductVariantDto(
  int Id,
  decimal Price,
  decimal? CostPrice,
  string? Sku,
  string? Barcode,
  bool IsActive,
  int DisplayOrder,
  List<int> ValueIds);

public record ProductDto(
  int Id,
  int? CategoryId,
  string? CategoryName,
  string Name,
  string? Description,
  decimal Price,
  decimal? CostPrice,
  decimal? DiscountPrice,
  string? Sku,
  string? Barcode,
  bool IsActive,
  string? ImageUrl,
  bool IsAccompaniment,
  int? EstimatedPrepMinutes,
  List<ProductVariantGroupDto> VariantGroups,
  List<ProductVariantDto> Variants,
  List<int> AssignedOptionGroupIds,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
