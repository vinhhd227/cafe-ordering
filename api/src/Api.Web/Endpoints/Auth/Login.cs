using Api.Core.Entities.Identity;
using Api.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace Api.Web.Endpoints.Auth;

public class LoginRequest
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }
  public string? RefreshToken { get; init; }
  public int ExpiresIn { get; init; }
  public UserDto? User { get; init; }
}

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly ITokenService _tokenService;

  public LoginEndpoint(UserManager<ApplicationUser> userManager, ITokenService tokenService)
  {
    _userManager = userManager;
    _tokenService = tokenService;
  }

  public override void Configure()
  {
    Post("/api/auth/login");
    AllowAnonymous();
    Description(b => b
      .WithTags("Auth")
      .WithSummary("Login user")
      .WithDescription("Authenticate user and receive access and refresh tokens"));
  }

  public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
  {
    // Validate input
    if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
    {
      await SendAsync(new LoginResponse
      {
        Success = false,
        Message = "Email and password are required"
      }, 400, ct);
      return;
    }

    // Find user
    var user = await _userManager.FindByEmailAsync(req.Email);

    if (user == null || !user.IsActive)
    {
      await SendAsync(new LoginResponse
      {
        Success = false,
        Message = "Invalid credentials"
      }, 401, ct);
      return;
    }

    // Check password
    var passwordValid = await _userManager.CheckPasswordAsync(user, req.Password);

    if (!passwordValid)
    {
      await _userManager.AccessFailedAsync(user);
      await SendAsync(new LoginResponse
      {
        Success = false,
        Message = "Invalid credentials"
      }, 401, ct);
      return;
    }

    // Check lockout
    if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
    {
      await SendAsync(new LoginResponse
      {
        Success = false,
        Message = "Account is locked. Try again later."
      }, 403, ct);
      return;
    }

    // Reset access failed count on successful login
    await _userManager.ResetAccessFailedCountAsync(user);

    // Generate tokens
    var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
    var refreshToken = _tokenService.GenerateRefreshToken();

    // Store refresh token
    user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
    await _userManager.UpdateAsync(user);

    await SendOkAsync(new LoginResponse
    {
      Success = true,
      Message = "Login successful",
      AccessToken = accessToken,
      RefreshToken = refreshToken,
      ExpiresIn = 3600,
      User = new UserDto
      {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName
      }
    }, ct);
  }
}
