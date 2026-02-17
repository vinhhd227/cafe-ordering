using Api.Core.Entities.Identity;
using Api.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace Api.Web.Endpoints.Auth;

public class RefreshTokenRequest
{
  public string AccessToken { get; set; } = string.Empty;
  public string RefreshToken { get; set; } = string.Empty;
}

public class RefreshTokenResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }
  public string? RefreshToken { get; init; }
  public int ExpiresIn { get; init; }
}

public class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly ITokenService _tokenService;

  public RefreshTokenEndpoint(UserManager<ApplicationUser> userManager, ITokenService tokenService)
  {
    _userManager = userManager;
    _tokenService = tokenService;
  }

  public override void Configure()
  {
    Post("/api/auth/refresh-token");
    AllowAnonymous();
    Description(b => b
      .WithTags("Auth")
      .WithSummary("Refresh access token")
      .WithDescription("Get a new access token using a refresh token"));
  }

  public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
  {
    if (string.IsNullOrWhiteSpace(req.AccessToken) || string.IsNullOrWhiteSpace(req.RefreshToken))
    {
      await SendAsync(new RefreshTokenResponse
      {
        Success = false,
        Message = "Invalid token request"
      }, 400, ct);
      return;
    }

    // Get principal from expired token
    var principal = _tokenService.GetPrincipalFromExpiredToken(req.AccessToken);
    if (principal == null)
    {
      await SendAsync(new RefreshTokenResponse
      {
        Success = false,
        Message = "Invalid access token"
      }, 400, ct);
      return;
    }

    // Get user ID from claims
    var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
    {
      await SendAsync(new RefreshTokenResponse
      {
        Success = false,
        Message = "Invalid token claims"
      }, 400, ct);
      return;
    }

    // Find user
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user == null || !user.IsActive)
    {
      await SendAsync(new RefreshTokenResponse
      {
        Success = false,
        Message = "User not found"
      }, 404, ct);
      return;
    }

    // Validate refresh token
    if (user.RefreshToken != req.RefreshToken ||
        user.RefreshTokenExpiryTime <= DateTime.UtcNow)
    {
      await SendAsync(new RefreshTokenResponse
      {
        Success = false,
        Message = "Invalid or expired refresh token"
      }, 400, ct);
      return;
    }

    // Generate new tokens
    var newAccessToken = await _tokenService.GenerateAccessTokenAsync(user);
    var newRefreshToken = _tokenService.GenerateRefreshToken();

    // Update refresh token
    user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
    await _userManager.UpdateAsync(user);

    await SendOkAsync(new RefreshTokenResponse
    {
      Success = true,
      Message = "Token refreshed successfully",
      AccessToken = newAccessToken,
      RefreshToken = newRefreshToken,
      ExpiresIn = 3600
    }, ct);
  }
}
