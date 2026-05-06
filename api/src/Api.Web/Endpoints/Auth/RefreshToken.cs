using Api.UseCases.Auth.RefreshToken;
using Microsoft.AspNetCore.Hosting;

namespace Api.Web.Endpoints.Auth;

public sealed class RefreshTokenResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }

  /// <summary>UTC expiry time of the new access token.</summary>
  public DateTime? ExpiresAt { get; init; }
}

public class RefreshTokenEndpoint(IMediator mediator, IWebHostEnvironment env)
  : EndpointWithoutRequest<RefreshTokenResponse>
{
  public override void Configure()
  {
    Post("/api/auth/refresh");
    AllowAnonymous();
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var token = HttpContext.Request.Cookies["refreshToken"];

    if (string.IsNullOrWhiteSpace(token))
    {
      await Send.ResponseAsync(new RefreshTokenResponse { Success = false, Message = "Refresh token is required" }, 400, ct);
      return;
    }

    var result = await mediator.Send(new RefreshTokenCommand(token), ct);

    if (!result.IsSuccess)
    {
      await Send.ResponseAsync(new RefreshTokenResponse { Success = false, Message = "Invalid or expired refresh token" }, 401, ct);
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

    await Send.OkAsync(new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = result.Value.AccessToken,
      ExpiresAt = result.Value.ExpiresAt
    }, ct);
  }
}
