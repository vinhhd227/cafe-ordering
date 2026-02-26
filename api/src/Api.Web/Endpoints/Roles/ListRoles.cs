using Api.UseCases.Auth.GetRoles;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class ListRolesRequest
{
  [QueryParam] public int Page { get; set; } = 1;
  [QueryParam] public int PageSize { get; set; } = 20;
  [QueryParam] public string? Search { get; set; }
}

public class ListRolesEndpoint(IMediator mediator)
  : Ep.Req<ListRolesRequest>.Res<PagedRolesDto>
{
  public override void Configure()
  {
    Get("/api/admin/roles");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(ListRolesRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new GetRolesQuery(req.Page, req.PageSize, req.Search), ct);
    await this.SendResultAsync(result, ct);
  }
}
