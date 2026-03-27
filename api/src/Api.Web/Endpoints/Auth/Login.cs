using Api.UseCases.Auth.Login;
using Microsoft.AspNetCore.Hosting;

// Deprecated: use /api/admin/auth/login or /api/client/auth/login instead.

namespace Api.Web.Endpoints.Auth;

public sealed class LoginRequest
{
  /// <summary>Username (not email).</summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>Account password.</summary>
  public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }

  /// <summary>UTC expiry time of the access token.</summary>
  public DateTime? ExpiresAt { get; init; }
}

public class LoginEndpoint(IMediator mediator, IWebHostEnvironment env) : Ep.Req<LoginRequest>.Res<LoginResponse>
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
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
    {
      await SendAsync(new LoginResponse { Success = false, Message = "Username and password are required" }, 400, ct);
      return;
    }

    var result = await mediator.Send(new LoginCommand(req.Username, req.Password, AppType.Admin), ct);

    if (!result.IsSuccess)
    {
      await SendAsync(new LoginResponse { Success = false, Message = "Invalid credentials" }, 401, ct);
      return;
    }

    HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken, new CookieOptions
    {
      HttpOnly = true,
      Secure = !env.IsDevelopment(),
      SameSite = SameSiteMode.Strict,
      MaxAge = TimeSpan.FromDays(7),
      Path = "/api/auth"
    });

    await SendOkAsync(new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = result.Value.AccessToken,
      ExpiresAt = result.Value.ExpiresAt
    }, ct);
  }
}
