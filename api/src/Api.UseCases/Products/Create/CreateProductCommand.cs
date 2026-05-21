using Api.UseCases.Products.VariantGroups;
using Api.UseCases.Products.Variants;

namespace Api.UseCases.Products.Create;

public record CreateProductCommand(
  string Name,
  decimal Price,
  int? CategoryId = null,
  string? Description = null,
  string? ImageUrl = null,
  bool IsAccompaniment = false,
  int? EstimatedPrepMinutes = null,
  decimal? CostPrice = null,
  decimal? DiscountPrice = null,
  string? Sku = null,
  string? Barcode = null,
  IReadOnlyList<VariantGroupInput>? VariantGroups = null,
  IReadOnlyList<ProductVariantLabelInput>? Variants = null
) : ICommand<Result<int>>;
