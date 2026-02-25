using Api.UseCases.Interfaces;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Request payload for checking username availability.
/// </summary>
public sealed class CheckUsernameRequest
{
  /// <summary>Username to check for availability.</summary>
  public string Username { get; set; } = string.Empty;
}

/// <summary>
/// Response indicating whether the username is already taken.
/// </summary>
public sealed class CheckUsernameResponse
{
  /// <summary>True if the username/email is already registered.</summary>
  public bool Exists { get; init; }
}

public class CheckUsernameEndpoint(IIdentityService identityService)
  : Ep.Req<CheckUsernameRequest>.Res<CheckUsernameResponse>
{
  public override void Configure()
  {
    Post("/api/auth/check-username");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(CheckUsernameRequest req, CancellationToken ct)
  {
    var isAvailable = await identityService.IsUsernameAvailableAsync(req.Username);
    await SendOkAsync(new CheckUsernameResponse { Exists = !isAvailable }, ct);
  }
}
