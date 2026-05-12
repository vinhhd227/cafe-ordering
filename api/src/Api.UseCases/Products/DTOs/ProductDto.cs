namespace Api.UseCases.Products.DTOs;

public record ProductAttributeValueDto(
  int Id,
  string Label,
  decimal PriceAdjustment,
  bool IsDefault,
  int DisplayOrder);

public record ProductAttributeGroupDto(
  int Id,
  string Name,
  bool IsRequired,
  string SelectionType,
  int DisplayOrder,
  List<ProductAttributeValueDto> Values);

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
  List<ProductAttributeGroupDto> AttributeGroups,
  List<int> AssignedOptionGroupIds,
  DateTime CreatedAt,
  DateTime? UpdatedAt
);
