using Api.UseCases.Auth.GetRolePermissions;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class GetRolePermissionsRequest
{
  /// <summary>Role ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class GetRolePermissionsEndpoint(IMediator mediator)
  : Ep.Req<GetRolePermissionsRequest>.Res<List<RolePermissionDto>>
{
  public override void Configure()
  {
    Get("/api/admin/roles/{id}/permissions");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(GetRolePermissionsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetRolePermissionsQuery(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
