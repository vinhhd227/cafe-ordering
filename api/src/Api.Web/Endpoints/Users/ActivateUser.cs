using Api.UseCases.Auth.ActivateUser;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class ActivateUserRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class ActivateUserEndpoint(IMediator mediator)
  : Ep.Req<ActivateUserRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/users/{id}/activate");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(ActivateUserRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ActivateUserCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
