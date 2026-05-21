namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class ProductsByIdsWithVariantGroupsSpec : Specification<Product>
{
  public ProductsByIdsWithVariantGroupsSpec(IEnumerable<int> ids)
    => Query
      .Where(p => ids.Contains(p.Id))
      .Include(p => p.VariantGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.Variants)
        .ThenInclude(v => v.Values)
          .ThenInclude(v => v.Value)
      .Include(p => p.OptionGroupMappings)
        .ThenInclude(m => m.Group!)
          .ThenInclude(g => g.Values);
}
