namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Loads products by their IDs with Category eagerly included.
///   Used for category-name lookups during report aggregation.
/// </summary>
public class ProductsByIdsWithCategorySpec : Specification<Product>
{
  public ProductsByIdsWithCategorySpec(IEnumerable<int> ids)
    => Query
      .Where(p => ids.Contains(p.Id))
      .Include(p => p.Category);
}
