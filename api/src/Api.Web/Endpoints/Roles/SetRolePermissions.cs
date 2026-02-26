using Api.UseCases.Auth.SetRolePermissions;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class SetRolePermissionsRequest
{
  /// <summary>Role ID from route segment {id}.</summary>
  public Guid Id { get; set; }

  /// <summary>Full set of permission values to assign. Replaces existing permissions.</summary>
  public List<string> Permissions { get; set; } = [];
}

public class SetRolePermissionsEndpoint(IMediator mediator)
  : Ep.Req<SetRolePermissionsRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/roles/{id}/permissions");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(SetRolePermissionsRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new SetRolePermissionsCommand(req.Id, req.Permissions), ct);
    await this.SendResultAsync(result, ct);
  }
}
