using Api.UseCases.Auth.DeactivateUser;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class DeactivateUserRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class DeactivateUserEndpoint(IMediator mediator)
  : Ep.Req<DeactivateUserRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/users/{id}/deactivate");
    Policies("user.deactivate");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(DeactivateUserRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeactivateUserCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
