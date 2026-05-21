using Api.Core.Aggregates.CategoryAggregate;
using Api.Core.Aggregates.CategoryAggregate.Specifications;
using Api.Core.Aggregates.ProductAggregate;
using Api.Core.Aggregates.ProductAggregate.Specifications;
using Api.UseCases.Menu.DTOs;

namespace Api.UseCases.Menu.GetAdminMenu;

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
            p.IsAccompaniment,
            p.EstimatedPrepMinutes,
            p.VariantGroups
              .OrderBy(g => g.DisplayOrder)
              .Select(g => new MenuVariantGroupDto(
                g.Id,
                g.Name,
                g.IsRequired,
                g.SelectionType.ToString(),
                g.Values
                  .OrderBy(v => v.DisplayOrder)
                  .Select(v => new MenuVariantValueDto(v.Id, v.Label, v.Price, v.IsDefault))
                  .ToList()))
              .ToList(),
            p.Variants
              .OrderBy(v => v.DisplayOrder)
              .Select(v => new MenuProductVariantDto(
                v.Id,
                v.Price,
                v.IsActive,
                v.Values.Select(vv => vv.ProductVariantValueId).OrderBy(id => id).ToList()))
              .ToList(),
            p.OptionGroupMappings
              .Where(m => m.Group != null && m.Group.IsActive && !m.Group.IsDeleted)
              .OrderBy(m => m.DisplayOrder)
              .Select(m => new MenuOptionGroupDto(
                m.Group!.Id,
                m.Group.Name,
                m.Group.IsRequired,
                m.Group.AllowMultiple,
                m.Group.AllowQuantity,
                m.DisplayOrder,
                m.Group.Values
                  .OrderBy(v => v.DisplayOrder)
                  .Select(v => new MenuOptionValueDto(v.Id, v.Name, v.Price))
                  .ToList()))
              .ToList()))
          .ToList()))
      .ToList();

    return Result.Success(result);
  }
}
