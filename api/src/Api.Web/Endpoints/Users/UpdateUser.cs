using Api.UseCases.Auth.UpdateUser;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class UpdateUserRequest
{
  /// <summary>User ID from route segment {id}.</summary>
  public Guid Id { get; set; }

  public string FullName { get; set; } = string.Empty;
  public string? Email { get; set; }
}

public class UpdateUserEndpoint(IMediator mediator)
  : Ep.Req<UpdateUserRequest>.NoRes
{
  public override void Configure()
  {
    Put("/api/admin/users/{id}");
    Policies("AdminOnly");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(UpdateUserRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(
      new UpdateUserCommand(req.Id, req.FullName, req.Email), ct);
    await this.SendResultAsync(result, ct);
  }
}
