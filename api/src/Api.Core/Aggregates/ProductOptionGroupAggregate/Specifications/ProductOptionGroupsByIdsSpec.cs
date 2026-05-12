namespace Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

public class ProductOptionGroupsByIdsSpec : Specification<ProductOptionGroup>
{
  public ProductOptionGroupsByIdsSpec(IEnumerable<int> ids)
  {
    Query
      .Where(g => ids.Contains(g.Id) && !g.IsDeleted);
  }
}
