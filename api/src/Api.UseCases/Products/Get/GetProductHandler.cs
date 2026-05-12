using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Products.DTOs;

namespace Api.UseCases.Products.Get;

public class GetProductHandler(IReadRepositoryBase<Product> repository)
  : IQueryHandler<GetProductQuery, Result<ProductDto>>
{
  public async ValueTask<Result<ProductDto>> Handle(GetProductQuery request, CancellationToken ct)
  {
    var spec = new ProductByIdWithCategorySpec(request.ProductId);
    var product = await repository.FirstOrDefaultAsync(spec, ct);

    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found");

    return Result.Success(MapToDto(product));
  }

  internal static ProductDto MapToDto(Product product) => new(
    product.Id,
    product.CategoryId,
    product.Category?.Name,
    product.Name,
    product.Description,
    product.Price,
    product.CostPrice,
    product.DiscountPrice,
    product.Sku,
    product.Barcode,
    product.IsActive,
    product.ImageUrl,
    product.IsAccompaniment,
    product.EstimatedPrepMinutes,
    product.AttributeGroups
      .OrderBy(g => g.DisplayOrder)
      .Select(g => new ProductAttributeGroupDto(
        g.Id,
        g.Name,
        g.IsRequired,
        g.SelectionType.ToString(),
        g.DisplayOrder,
        g.Values
          .OrderBy(v => v.DisplayOrder)
          .Select(v => new ProductAttributeValueDto(v.Id, v.Label, v.PriceAdjustment, v.IsDefault, v.DisplayOrder))
          .ToList()))
      .ToList(),
    product.OptionGroupMappings
      .OrderBy(m => m.DisplayOrder)
      .Select(m => m.GroupId)
      .ToList(),
    product.CreatedAt,
    product.UpdatedAt);
}
