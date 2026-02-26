using Api.UseCases.Auth.UpdateRole;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class UpdateRoleRequest
{
  /// <summary>Role ID from route segment {id}.</summary>
  public Guid Id { get; set; }

  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
}

public class UpdateRoleEndpoint(IMediator mediator)
  : Ep.Req<UpdateRoleRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/roles/{id}");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(UpdateRoleRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new UpdateRoleCommand(req.Id, req.Name, req.Description), ct);
    await this.SendResultAsync(result, ct);
  }
}
