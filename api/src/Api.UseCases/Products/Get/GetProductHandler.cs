using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Products.DTOs;

namespace Api.UseCases.Products.Get;

/// <summary>
///   Handler lấy chi tiết Product theo Id
/// </summary>
public class GetProductHandler(IReadRepositoryBase<Product> repository)
  : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
  public async ValueTask<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken ct)
  {
    var spec = new ProductByIdWithCategorySpec(request.ProductId);
    var product = await repository.FirstOrDefaultAsync(spec, ct);

    if (product is null)
    {
      return Result.NotFound($"Product {request.ProductId} not found");
    }

    return new ProductDto(
      product.Id,
      product.CategoryId,
      product.Category?.Name,
      product.Name,
      product.Description,
      product.Price,
      product.IsActive,
      product.ImageUrl,
      product.HasTemperatureOption,
      product.HasIceLevelOption,
      product.HasSugarLevelOption,
      product.CreatedAt,
      product.UpdatedAt);
  }
}
