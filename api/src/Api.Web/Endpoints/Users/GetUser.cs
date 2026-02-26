using Api.UseCases.Auth.GetUser;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class GetUserRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }
}

public class GetUserEndpoint(IMediator mediator)
  : Ep.Req<GetUserRequest>.Res<UserDto>
{
  public override void Configure()
  {
    Get("/api/admin/users/{id}");
    Policies("user.read");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(GetUserRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new GetUserQuery(req.Id), ct);
    await this.SendResultAsync(result, ct);
  }
}
