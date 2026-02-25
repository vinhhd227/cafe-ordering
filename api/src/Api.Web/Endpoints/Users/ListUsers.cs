using Api.UseCases.Auth.GetUsers;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class ListUsersRequest
{
  [QueryParam] public int Page { get; set; } = 1;
  [QueryParam] public int PageSize { get; set; } = 20;
  [QueryParam] public string? Search { get; set; }
  [QueryParam] public string? Role { get; set; }
  [QueryParam] public bool? IsActive { get; set; }
}

public class ListUsersEndpoint(IMediator mediator)
  : Ep.Req<ListUsersRequest>.Res<PagedUsersDto>
{
  public override void Configure()
  {
    Get("/api/admin/users");
    Policies("StaffOrAdmin");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(ListUsersRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new GetUsersQuery(req.Page, req.PageSize, req.Search, req.Role, req.IsActive), ct);
    await this.SendResultAsync(result, ct);
  }
}
