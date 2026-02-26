using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Menu.DTOs;

namespace Api.UseCases.Menu.GetAdminMenu;

/// <summary>
///   Lấy toàn bộ menu cho admin: tất cả categories (kể cả inactive)
///   cùng tất cả products (kể cả inactive) của từng category.
///   Không cache — admin cần dữ liệu real-time.
/// </summary>
public class GetAdminMenuHandler(
  IReadRepositoryBase<Category> categoryRepository,
  IReadRepositoryBase<Product> productRepository)
  : Common.Interfaces.IQueryHandler<GetAdminMenuQuery, Result<List<AdminMenuCategoryDto>>>
{
  public async ValueTask<Result<List<AdminMenuCategoryDto>>> Handle(GetAdminMenuQuery request, CancellationToken ct)
  {
    var categories = await categoryRepository.ListAsync(new AllCategoriesSpec(), ct);
    var products   = await productRepository.ListAsync(new AllProductsSpec(), ct);

    var productsByCategory = products.ToLookup(p => p.CategoryId);

    var result = categories
      .Select(c => new AdminMenuCategoryDto(
        c.Id,
        c.Name,
        c.Description,
        c.IsActive,
        productsByCategory[c.Id]
          .Select(p => new AdminMenuProductDto(
            p.Id,
            p.CategoryId,
            p.Name,
            p.Description,
            p.Price,
            p.ImageUrl,
            p.IsActive,
            p.HasTemperatureOption,
            p.HasIceLevelOption,
            p.HasSugarLevelOption))
          .ToList()))
      .ToList();

    return Result.Success(result);
  }
}
