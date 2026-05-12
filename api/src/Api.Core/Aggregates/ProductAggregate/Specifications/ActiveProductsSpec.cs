namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class ActiveProductsSpec : Specification<Product>
{
  public ActiveProductsSpec()
  {
    Query
      .Where(p => p.IsActive && !p.IsDeleted)
      .Include(p => p.AttributeGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.OptionGroupMappings)
        .ThenInclude(m => m.Group!)
          .ThenInclude(g => g.Values)
      .OrderBy(p => p.Name);
  }
}
