using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Categories.DTOs;

namespace Api.UseCases.Categories.List;

/// <summary>
///   Handler lấy danh sách Categories
/// </summary>
public class ListCategoriesHandler(
  IReadRepositoryBase<Category> repository,
  IReadRepositoryBase<Product> productRepository)
  : IQueryHandler<ListCategoriesQuery, Result<List<CategoryDto>>>
{
  public async ValueTask<Result<List<CategoryDto>>> Handle(ListCategoriesQuery request, CancellationToken ct)
  {
    var categories = request.ActiveOnly
      ? await repository.ListAsync(new ActiveCategoriesSpec(), ct)
      : await repository.ListAsync(new AllCategoriesSpec(), ct);

    var products = await productRepository.ListAsync(new ProductsCountSpec(), ct);
    var countByCategory = products
      .Where(p => p.CategoryId.HasValue)
      .GroupBy(p => p.CategoryId!.Value)
      .ToDictionary(g => g.Key, g => g.Count());

    var dtos = categories
      .Select(c => new CategoryDto(
        c.Id,
        c.Name,
        c.Description,
        c.ImageUrl,
        c.SortOrder,
        c.IsActive,
        countByCategory.GetValueOrDefault(c.Id, 0),
        c.CreatedAt,
        c.UpdatedAt))
      .ToList();

    return Result.Success(dtos);
  }
}
