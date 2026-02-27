namespace Api.Core.Aggregates.ProductAggregate.Specifications;

/// <summary>
///   Danh sách Products phân trang, lọc chưa bị xóa, hỗ trợ search theo tên
/// </summary>
public class ProductsPagedSpec : Specification<Product>
{
  public ProductsPagedSpec(
    int page,
    int pageSize,
    string? searchTerm = null,
    bool? isActive = null,
    int? categoryId = null,
    decimal? minPrice = null,
    decimal? maxPrice = null)
  {
    Query
      .Where(p => !p.IsDeleted)
      .Include(p => p.Category);

    if (!string.IsNullOrWhiteSpace(searchTerm))
      Query.Where(p => p.Name.Contains(searchTerm));

    if (isActive.HasValue)
      Query.Where(p => p.IsActive == isActive.Value);

    if (categoryId.HasValue)
      Query.Where(p => p.CategoryId == categoryId.Value);

    if (minPrice.HasValue)
      Query.Where(p => p.Price >= minPrice.Value);

    if (maxPrice.HasValue)
      Query.Where(p => p.Price <= maxPrice.Value);

    Query
      .OrderBy(p => p.Id)
      .Skip((page - 1) * pageSize)
      .Take(pageSize);
  }
}
