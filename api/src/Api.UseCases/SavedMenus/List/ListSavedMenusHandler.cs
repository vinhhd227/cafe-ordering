using Api.Core.Aggregates.SavedMenuAggregate;
using Api.Core.Aggregates.SavedMenuAggregate.Specifications;
using Api.UseCases.SavedMenus.DTOs;

namespace Api.UseCases.SavedMenus.List;

public class ListSavedMenusHandler(IReadRepositoryBase<SavedMenu> repository)
  : IQueryHandler<ListSavedMenusQuery, Result<List<SavedMenuDto>>>
{
  public async ValueTask<Result<List<SavedMenuDto>>> Handle(ListSavedMenusQuery query, CancellationToken ct)
  {
    var menus = await repository.ListAsync(new AllSavedMenusSpec(), ct);

    var dtos = menus.Select(m => new SavedMenuDto(
      m.Id,
      m.Name,
      m.CafeName,
      m.Slogan,
      m.PrimaryColor,
      m.Layout,
      m.MenuTemplate,
      m.MenuFont,
      m.CategoryOrderJson,
      m.SelectedProductIdsJson,
      m.CreatedAt)).ToList();

    return Result.Success(dtos);
  }
}
