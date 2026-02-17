namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Danh sách Products phân trang, lọc chưa bị xóa, hỗ trợ search theo tên
/// </summary>
public class ProductsPagedSpec : Specification<Product>
{
  public ProductsPagedSpec(int page, int pageSize, string? searchTerm = null)
  {
    Query.Where(p => !p.IsDeleted);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
      Query.Where(p => p.Name.Contains(searchTerm));
    }

    Query
      .OrderByDescending(p => p.CreatedAt)
      .Skip((page - 1) * pageSize)
      .Take(pageSize);
  }
}
