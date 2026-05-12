using Api.Core.Aggregates.ProductOptionGroupAggregate;

namespace Api.UseCases.ProductOptionGroups.DTOs;

public static class ProductOptionGroupMapper
{
  public static ProductOptionGroupDto ToDto(
    this ProductOptionGroup g,
    IEnumerable<LinkedProductDto>? linkedProducts = null) => new(
    g.Id,
    g.Name,
    g.IsRequired,
    g.AllowMultiple,
    g.AllowQuantity,
    g.IsActive,
    g.DisplayOrder,
    g.Values
      .OrderBy(v => v.DisplayOrder)
      .Select(v => new ProductOptionValueDto(v.Id, v.Name, v.Price, v.CostPrice, v.IsInStock, v.DisplayOrder))
      .ToList(),
    linkedProducts?.ToList() ?? []);
}
