namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class AllProductsSpec : Specification<Product>
{
  public AllProductsSpec()
  {
    Query
      .Where(p => !p.IsDeleted)
      .Include(p => p.AttributeGroups)
        .ThenInclude(g => g.Values)
      .OrderBy(p => p.Name);
  }
}
