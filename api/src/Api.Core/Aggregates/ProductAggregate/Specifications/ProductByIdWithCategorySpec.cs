namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Láº¥y Product theo Id kÃ¨m Category vÃ  OptionGroups (chÆ°a bá»‹ xÃ³a)
/// </summary>
public class ProductByIdWithCategorySpec : Specification<Product>
{
  public ProductByIdWithCategorySpec(int productId)
  {
    Query
      .Where(p => p.Id == productId && !p.IsDeleted)
      .Include(p => p.Category)
      .Include(p => p.VariantGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.Variants)
        .ThenInclude(v => v.Values)
          .ThenInclude(v => v.Value)
      .Include(p => p.OptionGroupMappings);
  }
}
