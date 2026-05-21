using Api.Core.Aggregates.OrderAggregate;
using Api.Core.Aggregates.ProductAggregate;

namespace Api.UseCases.Orders.DTOs;

public record ProductVariantPricing(
  decimal UnitPrice,
  IReadOnlyList<OrderItemOptionData> Options);

public static class ProductVariantPricingResolver
{
  public static ProductVariantPricing? Resolve(Product product, List<int>? selectedValueIds)
  {
    if (selectedValueIds is null || selectedValueIds.Count == 0)
    {
      if (product.Variants.Any())
        return null;

      return new ProductVariantPricing(product.Price, []);
    }

    var selectedIds = selectedValueIds
      .Distinct()
      .OrderBy(id => id)
      .ToList();

    var selectedOptions = new List<(ProductVariantGroup Group, ProductVariantValue Value)>();

    foreach (var valueId in selectedIds)
    {
      var group = product.VariantGroups
        .FirstOrDefault(g => g.Values.Any(v => v.Id == valueId));

      if (group is null)
        return null;

      if (selectedOptions.Any(x => x.Group.Id == group.Id))
        return null;

      var value = group.Values.First(v => v.Id == valueId);
      selectedOptions.Add((group, value));
    }

    if (product.Variants.Any())
    {
      var variant = product.Variants
        .Where(v => v.IsActive)
        .FirstOrDefault(v => v.Values
          .Select(vv => vv.ProductVariantValueId)
          .OrderBy(id => id)
          .SequenceEqual(selectedIds));

      if (variant is null)
        return null;

      return new ProductVariantPricing(
        variant.Price,
        selectedOptions
          .Select(x => new OrderItemOptionData(x.Value.Id, x.Group.Name, x.Value.Label, 0))
          .ToList());
    }

    return new ProductVariantPricing(
      product.Price,
      selectedOptions
        .Select(x => new OrderItemOptionData(x.Value.Id, x.Group.Name, x.Value.Label, x.Value.Price))
        .ToList());
  }
}
