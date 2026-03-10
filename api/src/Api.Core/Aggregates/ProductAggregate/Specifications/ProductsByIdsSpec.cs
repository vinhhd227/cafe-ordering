namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class ProductsByIdsSpec : Specification<Product>
{
  public ProductsByIdsSpec(IEnumerable<int> ids)
    => Query.Where(p => ids.Contains(p.Id));
}
