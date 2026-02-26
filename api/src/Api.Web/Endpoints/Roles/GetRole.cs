using Api.UseCases.Auth.GetRole;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class GetRoleRequest
{
  /// <summary>Role ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class GetRoleEndpoint(IMediator mediator)
  : Ep.Req<GetRoleRequest>.Res<RoleDto>
{
  public override void Configure()
  {
    Get("/api/admin/roles/{id}");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(GetRoleRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetRoleQuery(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
