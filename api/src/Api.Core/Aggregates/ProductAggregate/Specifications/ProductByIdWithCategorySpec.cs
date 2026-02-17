namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy Product theo Id kèm Category (chưa bị xóa)
/// </summary>
public class ProductByIdWithCategorySpec : Specification<Product>
{
  public ProductByIdWithCategorySpec(int productId)
  {
    Query
      .Where(p => p.Id == productId && !p.IsDeleted)
      .Include(p => p.Category);
  }
}
