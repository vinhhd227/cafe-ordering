using Api.UseCases.Auth.DeactivateUser;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Staff;

public sealed class DeactivateUserRequest
{
  /// <summary>The user ID from the route.</summary>
  public Guid Id { get; set; }
}

public class DeactivateUserEndpoint(IMediator mediator) : Ep.Req<DeactivateUserRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/staff/accounts/{id}/deactivate");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Staff"));
  }

  public override async Task HandleAsync(DeactivateUserRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new DeactivateUserCommand(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
