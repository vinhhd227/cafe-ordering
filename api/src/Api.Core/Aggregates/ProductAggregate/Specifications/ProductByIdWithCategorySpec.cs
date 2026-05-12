namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy Product theo Id kèm Category và OptionGroups (chưa bị xóa)
/// </summary>
public class ProductByIdWithCategorySpec : Specification<Product>
{
  public ProductByIdWithCategorySpec(int productId)
  {
    Query
      .Where(p => p.Id == productId && !p.IsDeleted)
      .Include(p => p.Category)
      .Include(p => p.AttributeGroups)
        .ThenInclude(g => g.Values)
      .Include(p => p.OptionGroupMappings);
  }
}
