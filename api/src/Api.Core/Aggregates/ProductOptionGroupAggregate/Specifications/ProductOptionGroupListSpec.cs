namespace Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

public class ProductOptionGroupListSpec : Specification<ProductOptionGroup>
{
  public ProductOptionGroupListSpec(string? searchTerm, bool? isActive)
  {
    Query
      .Where(g => !g.IsDeleted)
      .Include(g => g.Values)
      .Include(g => g.Mappings)
      .OrderBy(g => g.DisplayOrder)
      .ThenBy(g => g.Name);

    if (!string.IsNullOrWhiteSpace(searchTerm))
      Query.Where(g => g.Name.Contains(searchTerm));

    if (isActive.HasValue)
      Query.Where(g => g.IsActive == isActive.Value);
  }
}
