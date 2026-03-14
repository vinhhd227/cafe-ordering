namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Lấy tất cả Products đang active (chưa bị xóa) kèm Category — dùng cho promotion tree
/// </summary>
public class ActiveProductsWithCategorySpec : Specification<Product>
{
  public ActiveProductsWithCategorySpec()
  {
    Query
      .Where(p => p.IsActive && !p.IsDeleted)
      .Include(p => p.Category)
      .OrderBy(p => p.Category!.Name)
      .ThenBy(p => p.Name);
  }
}
