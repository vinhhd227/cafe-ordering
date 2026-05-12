using Api.UseCases.Products.OptionGroups;

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
  IReadOnlyList<AttributeGroupInput>? AttributeGroups = null
) : ICommand<Result<int>>;
