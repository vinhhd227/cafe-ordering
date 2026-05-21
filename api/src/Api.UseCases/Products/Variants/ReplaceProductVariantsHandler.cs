using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;

namespace Api.UseCases.Products.Variants;

public class ReplaceProductVariantsHandler(IRepositoryBase<Product> repository)
  : ICommandHandler<ReplaceProductVariantsCommand, Result<List<ProductVariantResultDto>>>
{
  public async ValueTask<Result<List<ProductVariantResultDto>>> Handle(
    ReplaceProductVariantsCommand request, CancellationToken ct)
  {
    var product = await repository.FirstOrDefaultAsync(new ProductByIdWithCategorySpec(request.ProductId), ct);
    if (product is null)
      return Result.NotFound($"Product {request.ProductId} not found.");

    var validValueIds = product.VariantGroups
      .SelectMany(g => g.Values.Select(v => v.Id))
      .ToHashSet();

    var signatures = new HashSet<string>();
    var variantData = new List<ProductVariantData>();

    foreach (var input in request.Variants)
    {
      var ids = input.ValueIds
        .Distinct()
        .OrderBy(id => id)
        .ToList();

      if (ids.Count == 0)
        return Result.Invalid(new ValidationError("Variants", "Each variant must contain at least one option value."));

      if (ids.Any(id => !validValueIds.Contains(id)))
        return Result.Invalid(new ValidationError("Variants", "Variant contains option values that do not belong to this product."));

      var groupIds = product.VariantGroups
        .Where(g => g.Values.Any(v => ids.Contains(v.Id)))
        .Select(g => g.Id)
        .ToList();

      if (groupIds.Count != ids.Count || groupIds.Distinct().Count() != groupIds.Count)
        return Result.Invalid(new ValidationError("Variants", "Each variant can contain only one value per variant option group."));

      var signature = string.Join(",", ids);
      if (!signatures.Add(signature))
        return Result.Invalid(new ValidationError("Variants", "Duplicate variant combination."));

      variantData.Add(new ProductVariantData(
        ids,
        input.Price,
        input.CostPrice,
        input.Sku,
        input.Barcode,
        input.IsActive));
    }

    product.ReplaceVariants(variantData);
    await repository.UpdateAsync(product, ct);

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
        v.Values.Select(vv => vv.ProductVariantValueId).OrderBy(id => id).ToList()))
      .ToList());
  }
}
