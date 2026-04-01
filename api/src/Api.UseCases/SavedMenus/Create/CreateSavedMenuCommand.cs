using Api.UseCases.SavedMenus.DTOs;

namespace Api.UseCases.SavedMenus.Create;

public record CreateSavedMenuCommand(
  string Name,
  string CafeName,
  string Slogan,
  string PrimaryColor,
  string Layout,
  string MenuTemplate,
  string MenuFont,
  string CategoryOrderJson,
  string SelectedProductIdsJson
) : ICommand<Result<SavedMenuDto>>;
