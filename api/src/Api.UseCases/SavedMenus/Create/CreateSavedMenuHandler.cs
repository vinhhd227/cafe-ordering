using Api.Core.Aggregates.SavedMenuAggregate;
using Api.UseCases.SavedMenus.DTOs;

namespace Api.UseCases.SavedMenus.Create;

public class CreateSavedMenuHandler(IRepositoryBase<SavedMenu> repository)
  : ICommandHandler<CreateSavedMenuCommand, Result<SavedMenuDto>>
{
  public async ValueTask<Result<SavedMenuDto>> Handle(CreateSavedMenuCommand request, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(request.Name))
      return Result.Invalid(new ValidationError("Name", "Menu name is required."));

    var menu = SavedMenu.Create(
      request.Name,
      request.CafeName,
      request.Slogan,
      request.PrimaryColor,
      request.Layout,
      request.MenuTemplate,
      request.CategoryOrderJson,
      request.SelectedProductIdsJson);

    await repository.AddAsync(menu, ct);

    return Result.Success(new SavedMenuDto(
      menu.Id,
      menu.Name,
      menu.CafeName,
      menu.Slogan,
      menu.PrimaryColor,
      menu.Layout,
      menu.MenuTemplate,
      menu.CategoryOrderJson,
      menu.SelectedProductIdsJson,
      menu.CreatedAt));
  }
}
