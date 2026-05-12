namespace Api.Core.Aggregates.ProductAggregate.Specifications;

public class ProductByIdWithOptionGroupMappingsSpec : Specification<Product>
{
  public ProductByIdWithOptionGroupMappingsSpec(int productId)
  {
    Query
      .Where(p => p.Id == productId && !p.IsDeleted)
      .Include(p => p.OptionGroupMappings);
  }
}
