using Api.Core.Entities.Identity;
using Api.Web.Services;
using Microsoft.AspNetCore.Identity;

namespace Api.Web.Endpoints.Auth;

public class RegisterRequest
{
  public string Email { get; set; } = string.Empty;
  public string Password { get; set; } = string.Empty;
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
}

public class RegisterResponse
{
  public bool Success { get; init; }
  public string Message { get; init; } = string.Empty;
  public string? AccessToken { get; init; }
  public string? RefreshToken { get; init; }
  public int ExpiresIn { get; init; }
  public UserDto? User { get; init; }
}

public class UserDto
{
  public int Id { get; init; }
  public string Email { get; init; } = string.Empty;
  public string FirstName { get; init; } = string.Empty;
  public string LastName { get; init; } = string.Empty;
  public string FullName { get; init; } = string.Empty;
}

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;
  private readonly ITokenService _tokenService;

  public RegisterEndpoint(UserManager<ApplicationUser> userManager, ITokenService tokenService)
  {
    _userManager = userManager;
    _tokenService = tokenService;
  }

  public override void Configure()
  {
    Post("/api/auth/register");
    AllowAnonymous();
    Description(b => b
      .WithTags("Auth")
      .WithSummary("Register a new user")
      .WithDescription("Create a new user account and receive access and refresh tokens"));
  }

  public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
  {
    // Validate input
    if (string.IsNullOrWhiteSpace(req.Email) ||
        string.IsNullOrWhiteSpace(req.Password) ||
        string.IsNullOrWhiteSpace(req.FirstName) ||
        string.IsNullOrWhiteSpace(req.LastName))
    {
      await SendAsync(new RegisterResponse
      {
        Success = false,
        Message = "All fields are required"
      }, 400, ct);
      return;
    }

    // Check if email already exists
    var existingUser = await _userManager.FindByEmailAsync(req.Email);
    if (existingUser != null)
    {
      await SendAsync(new RegisterResponse
      {
        Success = false,
        Message = "Email already registered"
      }, 400, ct);
      return;
    }

    // Create user
    var user = ApplicationUser.Create(
      userName: req.Email.Split('@')[0], // Use email prefix as username
      email: req.Email,
      firstName: req.FirstName,
      lastName: req.LastName
    );

    var result = await _userManager.CreateAsync(user, req.Password);

    if (!result.Succeeded)
    {
      await SendAsync(new RegisterResponse
      {
        Success = false,
        Message = string.Join(", ", result.Errors.Select(e => e.Description))
      }, 400, ct);
      return;
    }

    // Assign default role
    await _userManager.AddToRoleAsync(user, "Customer");

    // Auto-login after registration
    var accessToken = await _tokenService.GenerateAccessTokenAsync(user);
    var refreshToken = _tokenService.GenerateRefreshToken();

    user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7));
    await _userManager.UpdateAsync(user);

    await SendOkAsync(new RegisterResponse
    {
      Success = true,
      Message = "Registration successful",
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
