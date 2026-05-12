using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Create;

public class CreateProductHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<CreateProductCommand, Result<int>>
{
  public async ValueTask<Result<int>> Handle(CreateProductCommand request, CancellationToken ct)
  {
    var product = Product.Create(
      request.Name,
      request.Price,
      request.CategoryId,
      request.Description,
      request.ImageUrl,
      request.IsAccompaniment,
      request.CostPrice,
      request.DiscountPrice,
      request.Sku,
      request.Barcode);

    product.SetEstimatedPrepTime(request.EstimatedPrepMinutes);

    // Gán attribute groups trước khi AddAsync để EF Core persist tất cả
    // trong cùng một SaveChanges — tự động atomic, không cần transaction riêng.
    if (request.AttributeGroups is { Count: > 0 })
    {
      var groupData = request.AttributeGroups
        .Select(g => new ProductAttributeGroupData(
          g.Name,
          g.IsRequired,
          g.SelectionType,
          g.Values
            .Select(v => new ProductAttributeValueData(v.Label, v.PriceAdjustment, v.IsDefault))
            .ToList()))
        .ToList();

      product.ReplaceAttributeGroups(groupData);
    }

    await repository.AddAsync(product, ct);

    return Result.Success(product.Id);
  }
}
