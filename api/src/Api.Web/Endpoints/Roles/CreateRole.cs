using Api.UseCases.Auth.CreateRole;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Roles;

public sealed class CreateRoleRequest
{
  public string Name { get; set; } = string.Empty;
  public string? Description { get; set; }
}

public class CreateRoleEndpoint(IMediator mediator)
  : Ep.Req<CreateRoleRequest>.NoRes
{
  public override void Configure()
  {
    Post("/api/admin/roles");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Roles"));
  }

  public override async Task HandleAsync(CreateRoleRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new CreateRoleCommand(req.Name, req.Description), ct);
    await this.SendResultAsync(result, ct);
  }
}
