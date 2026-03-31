using Api.UseCases.SavedMenus.Create;
using Api.UseCases.SavedMenus.DTOs;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.SavedMenus;

public sealed class CreateSavedMenuRequest
{
  public string Name { get; set; } = string.Empty;
  public string CafeName { get; set; } = string.Empty;
  public string Slogan { get; set; } = string.Empty;
  public string PrimaryColor { get; set; } = "#10b981";
  public string Layout { get; set; } = "2col";
  public string MenuTemplate { get; set; } = "default";
  public string CategoryOrderJson { get; set; } = "[]";
  public string SelectedProductIdsJson { get; set; } = "[]";
}

public class CreateSavedMenu(IMediator mediator) : Endpoint<CreateSavedMenuRequest, SavedMenuDto>
{
  public override void Configure()
  {
    Post("/api/admin/saved-menus");
    Policies("utility.create");
    DontAutoTag();
    Description(b => b.WithTags("Saved Menus"));
  }

  public override async Task HandleAsync(CreateSavedMenuRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new CreateSavedMenuCommand(
      req.Name,
      req.CafeName,
      req.Slogan,
      req.PrimaryColor,
      req.Layout,
      req.MenuTemplate,
      req.CategoryOrderJson,
      req.SelectedProductIdsJson), ct);

    await this.SendResultAsync(result, ct);
  }
}
