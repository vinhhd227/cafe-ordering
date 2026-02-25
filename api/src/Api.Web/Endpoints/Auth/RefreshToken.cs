using Api.UseCases.Auth.RefreshToken;

namespace Api.Web.Endpoints.Auth;

public sealed class RefreshTokenRequest
{
  /// <summary>The refresh token received at login or from the last refresh call.</summary>
  public string RefreshToken { get; set; } = string.Empty;
}

public sealed class RefreshTokenResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }
  public string? RefreshToken { get; init; }

  /// <summary>UTC expiry time of the new refresh token.</summary>
  public DateTime? ExpiresAt { get; init; }
}

public class RefreshTokenEndpoint(IMediator mediator)
  : Ep.Req<RefreshTokenRequest>.Res<RefreshTokenResponse>
{
  public override void Configure()
  {
    Post("/api/auth/refresh");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.RefreshToken))
    {
      await SendAsync(new RefreshTokenResponse { Success = false, Message = "Refresh token is required" }, 400, ct);
      return;
    }

    var result = await mediator.Send(new RefreshTokenCommand(req.RefreshToken), ct);

    if (!result.IsSuccess)
    {
      await SendAsync(new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token" }, 401, ct);
      return;
    }

    await SendOkAsync(new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = result.Value.AccessToken,
      RefreshToken = result.Value.RefreshToken,
      ExpiresAt = result.Value.ExpiresAt
    }, ct);
  }
}
