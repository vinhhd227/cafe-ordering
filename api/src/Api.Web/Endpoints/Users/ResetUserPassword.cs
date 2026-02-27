using Api.UseCases.Auth.ResetUserPassword;
using Api.UseCases.Interfaces;
using Api.Web.Extensions;

namespace Api.Web.Endpoints.Users;

public sealed class ResetUserPasswordRequest
{
  public Guid Id { get; set; }
}

public sealed class ResetUserPasswordResponse
{
  public string Username { get; init; } = string.Empty;
  public string TemporaryPassword { get; init; } = string.Empty;
}

public class ResetUserPasswordEndpoint(IMediator mediator)
  : Ep.Req<ResetUserPasswordRequest>.Res<ResetUserPasswordResponse>
{
  public override void Configure()
  {
    Post("/api/admin/users/{id}/reset-password");
    Policies("user.resetPassword");
    DontAutoTag();
    Description(b => b.WithTags("Users"));
  }

  public override async Task HandleAsync(ResetUserPasswordRequest req, CancellationToken ct)
  {
    var result = await mediator.Send(new ResetUserPasswordCommand(req.Id), ct);

    if (!result.IsSuccess)
    {
      await this.SendResultAsync(
        Result<ResetUserPasswordResponse>.Error(string.Join("; ", result.Errors)), ct);
      return;
    }

    await SendAsync(new ResetUserPasswordResponse
    {
      Username = result.Value.Username,
      TemporaryPassword = result.Value.TemporaryPassword
    }, 200, ct);
  }
}
