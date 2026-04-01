namespace Api.UseCases.SavedMenus.DTOs;

public record SavedMenuDto(
  int Id,
  string Name,
  string CafeName,
  string Slogan,
  string PrimaryColor,
  string Layout,
  string MenuTemplate,
  string MenuFont,
  string CategoryOrderJson,
  string SelectedProductIdsJson,
  DateTime CreatedAt
);
