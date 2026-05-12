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
              .ToList()))
          .ToList()))
      .ToList();

    return Result.Success(result);
  }
}
