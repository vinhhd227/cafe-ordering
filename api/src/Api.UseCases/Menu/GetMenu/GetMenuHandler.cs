using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Menu.DTOs;

namespace Api.UseCases.Menu.GetMenu;

/// <summary>
///   Lấy menu: danh sách categories đang active cùng các products đang active của từng category
/// </summary>
public class GetMenuHandler(
  IReadRepositoryBase<Category> categoryRepository,
  IReadRepositoryBase<Product> productRepository)
  : IQueryHandler<GetMenuQuery, Result<List<MenuCategoryDto>>>
{
  public async ValueTask<Result<List<MenuCategoryDto>>> Handle(GetMenuQuery request, CancellationToken ct)
  {
    var categories = await categoryRepository.ListAsync(new ActiveCategoriesSpec(), ct);
    var products = await productRepository.ListAsync(new ActiveProductsSpec(), ct);

    var productsByCategory = products.ToLookup(p => p.CategoryId);

    var result = categories
      .Select(c => new MenuCategoryDto(
        c.Id,
        c.Name,
        productsByCategory[c.Id]
          .Select(p => new MenuProductDto(
            p.Id,
            p.Name,
            p.Description,
            p.Price,
            p.ImageUrl,
            p.HasTemperatureOption,
            p.HasIceLevelOption,
            p.HasSugarLevelOption))
          .ToList()))
      .Where(c => c.Products.Count > 0)
      .ToList();

    return Result.Success(result);
  }
}
