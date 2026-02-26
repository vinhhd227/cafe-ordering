using Api.UseCases.Auth.DeleteRole;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class DeleteRoleRequest
{
  /// <summary>Role ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class DeleteRoleEndpoint(IMediator mediator)
  : Ep.Req<DeleteRoleRequest>.NoRes
{
  public override void Configure()
  {
    Delete("/api/admin/roles/{id}");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(DeleteRoleRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeleteRoleCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
