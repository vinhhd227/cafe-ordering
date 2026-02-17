using Api.Core.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Web.Endpoints.Auth;

public class GetCurrentUserResponse
{
  public bool Success { get; init; }
  public UserDto? User { get; init; }
  public List<string> Roles { get; init; } = new();
}

public class GetCurrentUserEndpoint : EndpointWithoutRequest<GetCurrentUserResponse>
{
  private readonly UserManager<ApplicationUser> _userManager;

  public GetCurrentUserEndpoint(UserManager<ApplicationUser> userManager)
  {
    _userManager = userManager;
  }

  public override void Configure()
  {
    Get("/api/auth/me");
    Description(b => b
      .WithTags("Auth")
      .WithSummary("Get current user")
      .WithDescription("Get current authenticated user information"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    // Get user ID from claims
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    // Find user
    var user = await _userManager.FindByIdAsync(userId.ToString());
    if (user == null || !user.IsActive)
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    // Get roles
    var roles = await _userManager.GetRolesAsync(user);

    await SendOkAsync(new GetCurrentUserResponse
    {
      Success = true,
      User = new UserDto
      {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        FullName = user.FullName
      },
      Roles = roles.ToList()
    }, ct);
  }
}
