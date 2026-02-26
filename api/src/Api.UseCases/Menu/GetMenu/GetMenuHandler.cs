using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Menu.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Api.UseCases.Menu.GetMenu;

/// <summary>
///   Lấy menu public: chỉ active categories + active products.
///   Kết quả được cache trong IMemoryCache; tự động vô hiệu hoá khi
///   có domain event thay đổi category/product.
/// </summary>
public class GetMenuHandler(
  IReadRepositoryBase<Category> categoryRepository,
  IReadRepositoryBase<Product> productRepository,
  IMemoryCache cache)
  : IQueryHandler<GetMenuQuery, Result<List<MenuCategoryDto>>>
{
  public async ValueTask<Result<List<MenuCategoryDto>>> Handle(GetMenuQuery request, CancellationToken ct)
  {
    // ── Cache hit ────────────────────────────────────────────────
    if (cache.TryGetValue(MenuCacheKeys.PublicMenu, out List<MenuCategoryDto>? cached) && cached is not null)
    {
      return Result.Success(cached);
    }

    // ── Cache miss → query DB ────────────────────────────────────
    var categories = await categoryRepository.ListAsync(new ActiveCategoriesSpec(), ct);
    var products   = await productRepository.ListAsync(new ActiveProductsSpec(), ct);

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

    // ── Store in cache (1h fallback expiry) ──────────────────────
    cache.Set(MenuCacheKeys.PublicMenu, result,
      new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));

    return Result.Success(result);
  }
}
