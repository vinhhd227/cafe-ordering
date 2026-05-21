using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Products.Variants;

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

    if (request.VariantGroups is { Count: > 0 })
    {
      var groupData = request.VariantGroups
        .Select(g => new ProductVariantGroupData(
          g.Name,
          g.IsRequired,
          g.SelectionType,
          g.Values
            .Select(v => new ProductVariantValueData(v.Label, v.Price, v.IsDefault))
            .ToList()))
        .ToList();

      product.ReplaceVariantGroups(groupData);
    }

    await repository.AddAsync(product, ct);

    if (request.Variants is { Count: > 0 })
    {
      var saved = await repository.FirstOrDefaultAsync(new ProductByIdWithCategorySpec(product.Id), ct);
      if (saved is null)
        return Result.NotFound($"Product {product.Id} not found");

      var variantData = ProductVariantLabelResolver.Resolve(saved, request.Variants);
      if (!variantData.IsSuccess)
        return Result.Invalid(variantData.ValidationErrors);

      saved.ReplaceVariants(variantData.Value);
      await repository.UpdateAsync(saved, ct);
    }

    return Result.Success(product.Id);
  }
}
