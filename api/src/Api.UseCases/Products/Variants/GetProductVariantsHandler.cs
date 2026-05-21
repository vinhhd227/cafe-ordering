using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Products.Variants;

public class GetProductVariantsHandler(IReadRepositoryBase<Product> repository)
  : IQueryHandler<GetProductVariantsQuery, Result<List<ProductVariantResultDto>>>
{
  public async ValueTask<Result<List<ProductVariantResultDto>>> Handle(
    GetProductVariantsQuery request, CancellationToken ct)
  {
    var product = await repository.FirstOrDefaultAsync(new ProductByIdWithCategorySpec(request.ProductId), ct);
    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    return Result.Success(product.Variants
      .OrderBy(v => v.DisplayOrder)
      .Select(v => new ProductVariantResultDto(
        v.Id,
        v.Price,
        v.CostPrice,
        v.Sku,
        v.Barcode,
        v.IsActive,
        v.DisplayOrder,
        v.Values
          .OrderBy(vv => vv.Value?.GroupId)
          .ThenBy(vv => vv.Value?.DisplayOrder)
          .Select(vv => vv.ProductVariantValueId)
          .ToList()))
      .ToList());
  }
}
