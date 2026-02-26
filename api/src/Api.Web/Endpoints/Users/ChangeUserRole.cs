using Api.UseCases.Auth.ChangeUserRole;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class ChangeUserRoleRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }

  public string Role { get; set; } = string.Empty;
}

public class ChangeUserRoleEndpoint(IMediator mediator)
  : Ep.Req<ChangeUserRoleRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/users/{id}/roles");
    Policies("user.update");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(ChangeUserRoleRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new ChangeUserRoleCommand(req.Id, req.Role), ct);
    await this.SendResultAsync(result, ct);
  }
}
