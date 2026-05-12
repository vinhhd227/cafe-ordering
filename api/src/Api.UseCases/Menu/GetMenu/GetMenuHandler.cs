using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Menu.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace Api.UseCases.Menu.GetMenu;

public class GetMenuHandler(
  IReadRepositoryBase<Category> categoryRepository,
  IReadRepositoryBase<Product> productRepository,
  IMemoryCache cache)
  : IQueryHandler<GetMenuQuery, Result<List<MenuCategoryDto>>>
{
  public async ValueTask<Result<List<MenuCategoryDto>>> Handle(GetMenuQuery request, CancellationToken ct)
  {
    if (cache.TryGetValue(MenuCacheKeys.PublicMenu, out List<MenuCategoryDto>? cached) && cached is not null)
      return Result.Success(cached);

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
            p.IsAccompaniment,
            p.EstimatedPrepMinutes,
            p.AttributeGroups
              .OrderBy(g => g.DisplayOrder)
              .Select(g => new MenuAttributeGroupDto(
                g.Id,
                g.Name,
                g.IsRequired,
                g.SelectionType.ToString(),
                g.Values
                  .OrderBy(v => v.DisplayOrder)
                  .Select(v => new MenuAttributeValueDto(v.Id, v.Label, v.PriceAdjustment, v.IsDefault))
                  .ToList()))
              .ToList(),
            p.OptionGroupMappings
              .Where(m => m.Group is { IsActive: true, IsDeleted: false })
              .OrderBy(m => m.DisplayOrder)
              .Select(m => new MenuOptionGroupDto(
                m.Group!.Id,
                m.Group.Name,
                m.Group.IsRequired,
                m.Group.AllowMultiple,
                m.Group.AllowQuantity,
                m.DisplayOrder,
                m.Group.Values
                  .Where(v => v.IsInStock)
                  .OrderBy(v => v.DisplayOrder)
                  .Select(v => new MenuOptionValueDto(v.Id, v.Name, v.Price))
                  .ToList()))
              .ToList()))
          .ToList()))
      .Where(c => c.Products.Count > 0)
      .ToList();

    cache.Set(MenuCacheKeys.PublicMenu, result,
      new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromHours(1)));

    return Result.Success(result);
  }
}
