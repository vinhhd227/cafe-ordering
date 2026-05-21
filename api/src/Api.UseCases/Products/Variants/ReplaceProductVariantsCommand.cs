namespace Api.UseCases.Products.Variants;

public record ReplaceProductVariantsCommand(
  int ProductId,
  IReadOnlyList<ProductVariantInput> Variants)
  : ICommand<Result<List<ProductVariantResultDto>>>;
