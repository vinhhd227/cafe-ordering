namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class AllProductsSpec : Specification<Product>
{
  public AllProductsSpec()
  {
    Query
      .Where(p => !p.IsDeleted)
      .Include(p => p.VariantGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.Variants)
        .ThenInclude(v => v.Values)
      .Include(p => p.OptionGroupMappings)
        .ThenInclude(m => m.Group)
          .ThenInclude(g => g.Values)
      .OrderBy(p => p.Name);
  }
}
