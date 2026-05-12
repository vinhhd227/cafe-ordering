namespace Api.Core.Aggregates.ProductOptionGroupAggregate.Specifications;

public class ProductOptionGroupByIdWithValuesSpec : Specification<ProductOptionGroup>
{
  public ProductOptionGroupByIdWithValuesSpec(int id)
  {
    Query
      .Where(g => g.Id == id && !g.IsDeleted)
      .Include(g => g.Values)
      .Include(g => g.Mappings);
  }
}
