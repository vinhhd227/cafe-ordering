using Api.UseCases.Menu.DTOs;
using Api.UseCases.Menu.GetAdminMenu;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Menu;

/// <summary>
///   Admin menu endpoint — trả toàn bộ categories và products (bao gồm inactive).
///   Dùng cho trang Menu trong admin panel.
/// </summary>
public class GetAdmin(IMediator mediator) : EndpointWithoutRequest<List<AdminMenuCategoryDto>>
{
  public override void Configure()
  {
    Get("/api/admin/menu");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Menu"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new GetAdminMenuQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
