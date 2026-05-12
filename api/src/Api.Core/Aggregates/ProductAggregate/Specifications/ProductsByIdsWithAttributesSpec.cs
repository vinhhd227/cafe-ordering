namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class ProductsByIdsWithAttributesSpec : Specification<Product>
{
  public ProductsByIdsWithAttributesSpec(IEnumerable<int> ids)
    => Query
      .Where(p => ids.Contains(p.Id))
      .Include(p => p.AttributeGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.OptionGroupMappings)
        .ThenInclude(m => m.Group!)
          .ThenInclude(g => g.Values);
}
