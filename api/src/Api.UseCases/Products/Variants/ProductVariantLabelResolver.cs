using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Products.Variants;

public static class ProductVariantLabelResolver
{
  public static Result<List<ProductVariantData>> Resolve(
    Product product,
    IReadOnlyList<ProductVariantLabelInput> variants)
  {
    var groups = product.VariantGroups
      .OrderBy(g => g.DisplayOrder)
      .ToList();

    if (groups.Count == 0)
      return Result.Invalid(new ValidationError("Variants", "Variant groups must be saved before variants."));

    var signatures = new HashSet<string>(StringComparer.Ordinal);
    var variantData = new List<ProductVariantData>();

    foreach (var input in variants)
    {
      if (input.ValueLabels.Count != groups.Count)
        return Result.Invalid(new ValidationError("Variants", "Each variant must contain one value label per variant group."));

      var ids = new List<int>();

      for (var i = 0; i < groups.Count; i++)
      {
        var label = input.ValueLabels[i].Trim();
        var value = groups[i].Values.FirstOrDefault(v =>
          string.Equals(v.Label, label, StringComparison.OrdinalIgnoreCase));

        if (value is null)
          return Result.Invalid(new ValidationError("Variants", $"Variant value '{label}' does not belong to group '{groups[i].Name}'."));

        ids.Add(value.Id);
      }

      var signature = string.Join(",", ids.OrderBy(id => id));
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

    return Result.Success(variantData);
  }
}
