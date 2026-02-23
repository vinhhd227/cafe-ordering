using Api.UseCases.Menu.DTOs;
using Api.UseCases.Menu.GetMenu;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Menu;

public class Get(IMediator mediator) : EndpointWithoutRequest<List<MenuCategoryDto>>
{
  public override void Configure()
  {
    Get("/api/menu");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Menu"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var result = await mediator.Send(new GetMenuQuery(), ct);
    await this.SendResultAsync(result, ct);
  }
}
