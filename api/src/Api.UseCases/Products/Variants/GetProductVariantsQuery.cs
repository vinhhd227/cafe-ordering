namespace Api.UseCases.Products.Variants;

public record GetProductVariantsQuery(int ProductId) : IQuery<Result<List<ProductVariantResultDto>>>;
