using Api.UseCases.SavedMenus.DTOs;
using Api.UseCases.SavedMenus.List;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.SavedMenus;

public class ListSavedMenus(IMediator mediator) : EndpointWithoutRequest<List<SavedMenuDto>>
{
  public override void Configure()
  {
    Get("/api/admin/saved-menus");
    Policies("utility.read");
    DontAutoTag();
    Description(b => b.WithTags("Saved Menus"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new ListSavedMenusQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
