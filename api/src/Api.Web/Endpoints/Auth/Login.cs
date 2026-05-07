using Api.UseCases.Auth.Login;
using FluentValidation;

// Deprecated: use /api/admin/auth/login or /api/client/auth/login instead.

namespace Api.Web.Endpoints.Auth;

public sealed class LoginRequest
{
  /// <summary>Username (not email).</summary>
  public string Username { get; set; } = string.Empty;

  /// <summary>Account password.</summary>
  public string Password { get; set; } = string.Empty;

  /// <summary>Whether to issue a long-lived session (30 days) or a short session cookie (1 day).</summary>
  public bool RememberMe { get; set; }
}

public sealed class LoginResponse
{
  
  public bool Success { get; init; }
  public string? Message { get; init; }
  public string? ErrorCode { get; init; }
  public string? AccessToken { get; init; }

  /// <summary>UTC expiry time of the access token.</summary>
  public DateTime? ExpiresAt { get; init; }
}

sealed class LoginRequestValidator : Validator<LoginRequest>
{
  public LoginRequestValidator()
  {
    RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required");
    RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
  }
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
    var result = await mediator.Send(new LoginCommand(req.Username, req.Password, AppType.Admin, req.RememberMe), ct);

    if (!result.IsSuccess)
    {
      await Send.ResponseAsync(new LoginResponse { Success = false, Message = "Invalid credentials" }, 401, ct);
      return;
    }

    var cookieOptions = new CookieOptions
    {
      HttpOnly = true,
      Secure = !env.IsDevelopment(),
      SameSite = SameSiteMode.Strict,
      Path = "/api/auth"
    };
    if (result.Value.RememberMe)
      cookieOptions.MaxAge = TimeSpan.FromDays(30);

    HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken, cookieOptions);

    await Send.OkAsync(new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = result.Value.AccessToken,
      ExpiresAt = result.Value.ExpiresAt
    }, ct);
  }
}
