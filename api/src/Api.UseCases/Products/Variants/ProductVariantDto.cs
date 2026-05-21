namespace Api.UseCases.Products.Variants;

public record ProductVariantInput(
  IReadOnlyList<int> ValueIds,
  decimal Price,
  decimal? CostPrice,
  string? Sku,
  string? Barcode,
  bool IsActive);

public record ProductVariantLabelInput(
  IReadOnlyList<string> ValueLabels,
  decimal Price,
  decimal? CostPrice,
  string? Sku,
  string? Barcode,
  bool IsActive);

public record ProductVariantResultDto(
  int Id,
  decimal Price,
  decimal? CostPrice,
  string? Sku,
  string? Barcode,
  bool IsActive,
  int DisplayOrder,
  IReadOnlyList<int> ValueIds);
