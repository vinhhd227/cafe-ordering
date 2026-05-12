namespace Api.UseCases.Products.Update;

public record UpdateProductCommand(
  int ProductId,
  string Name,
  decimal Price,
  bool IsActive,
  int? CategoryId = null,
  bool IsAccompaniment = false,
  string? Description = null,
  string? ImageUrl = null,
  int? EstimatedPrepMinutes = null,
  decimal? CostPrice = null,
  decimal? DiscountPrice = null,
  string? Sku = null,
  string? Barcode = null
) : ICommand<Result>;
