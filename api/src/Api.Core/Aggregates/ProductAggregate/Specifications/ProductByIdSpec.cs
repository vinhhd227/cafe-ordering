namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy Product theo Id (chưa bị xóa)
/// </summary>
public class ProductByIdSpec : Specification<Product>
{
  public ProductByIdSpec(int productId)
  {
    Query
      .Where(p => p.Id == productId && !p.IsDeleted);
  }
}
