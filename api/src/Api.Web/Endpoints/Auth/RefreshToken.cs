using Api.UseCases.Interfaces;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Request payload for rotating an expiring access token.
/// </summary>
public sealed class RefreshTokenRequest
{
  /// <summary>
  /// The expired (or still-valid) access token issued at login.
  /// The user identity is extracted from this token's claims without re-validating its lifetime.
  /// </summary>
  public string AccessToken { get; set; } = string.Empty;

  /// <summary>
  /// The refresh token received at login or from the last refresh call.
  /// Must not have been revoked. After a successful refresh the old token is
  /// immediately invalidated and a new one is returned (token rotation).
  /// </summary>
  public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// New token pair issued after a successful refresh.
/// </summary>
public sealed class RefreshTokenResponse
{
  /// <summary>Indicates whether the refresh succeeded.</summary>
  public bool Success { get; init; }

  /// <summary>Human-readable status message.</summary>
  public string Message { get; init; } = string.Empty;

  /// <summary>New short-lived JWT access token.</summary>
  public string? AccessToken { get; init; }

  /// <summary>
  /// New refresh token. The previous refresh token is now revoked.
  /// Store this value and discard the old one.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>New access token lifetime in seconds.</summary>
  public int ExpiresIn { get; init; }
}

public class RefreshTokenEndpoint(IIdentityService identityService)
  : Ep.Req<RefreshTokenRequest>.Res<RefreshTokenResponse>
{
  public override void Configure()
  {
    Post("/api/auth/refresh-token");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.RefreshToken))
    {
      await SendAsync(new RefreshTokenResponse { Success = false, Message = "Invalid token request" }, 400, ct);
      return;
    }

    var result = await identityService.RefreshTokenAsync(req.AccessToken, req.RefreshToken);

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
      ExpiresIn = result.Value.ExpiresIn
    }, ct);
  }
}
