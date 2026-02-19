using Api.UseCases.Interfaces;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Request payload for authenticating a user.
/// </summary>
public sealed class LoginRequest
{
  /// <summary>Registered email address.</summary>
  public string Email { get; set; } = string.Empty;

  /// <summary>Account password.</summary>
  public string Password { get; set; } = string.Empty;

  /// <summary>
  /// Optional label identifying the calling device or browser session
  /// (e.g. "Chrome/Windows", "iPhone Safari", "Android App").
  /// Used to distinguish active sessions in the multi-device refresh-token store.
  /// </summary>
  public string? DeviceInfo { get; set; }
}

/// <summary>
/// Token pair returned after a successful login.
/// </summary>
public sealed class LoginResponse
{
  /// <summary>Indicates whether the login attempt succeeded.</summary>
  public bool Success { get; init; }

  /// <summary>Human-readable status message.</summary>
  public string Message { get; init; } = string.Empty;

  /// <summary>
  /// Short-lived JWT access token. Include in the <c>Authorization: Bearer &lt;token&gt;</c> header
  /// for all authenticated requests. Expires after the configured <c>Jwt:ExpiresMinutes</c>.
  /// </summary>
  public string? AccessToken { get; init; }

  /// <summary>
  /// Long-lived opaque refresh token (30-day TTL). Used exclusively with
  /// <c>POST /api/auth/refresh-token</c> to obtain a new access token.
  /// Each token is bound to one device session and is rotated on every refresh.
  /// </summary>
  public string? RefreshToken { get; init; }

  /// <summary>Access token lifetime in seconds.</summary>
  public int ExpiresIn { get; init; }

  /// <summary>Basic profile of the authenticated user.</summary>
  public UserDto? User { get; init; }
}

public class LoginEndpoint(IIdentityService identityService) : Ep.Req<LoginRequest>.Res<LoginResponse> 
{
  public override void Configure()
  {
    Post("/api/auth/login");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
      await SendAsync(new LoginResponse { Success = false, Message = "Email and password are required" }, 400, ct);
      return;
    }

    var result = await identityService.LoginAsync(req.Email, req.Password, req.DeviceInfo);

    if (!result.IsSuccess)
    {
      await SendAsync(new LoginResponse { Success = false, Message = "Invalid credentials" }, 401, ct);
      return;
    }

    await SendOkAsync(new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = result.Value.AccessToken,
      RefreshToken = result.Value.RefreshToken,
      ExpiresIn = result.Value.ExpiresIn
    }, ct);
  }
}
