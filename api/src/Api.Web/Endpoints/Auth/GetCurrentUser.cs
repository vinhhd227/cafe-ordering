using Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Api.Web.Endpoints.Auth;

/// <summary>
/// Profile and role information for the currently authenticated user.
/// </summary>
public sealed class GetCurrentUserResponse
{
  /// <summary>Always <c>true</c> when the request succeeds.</summary>
  public bool Success { get; init; }

  /// <summary>Basic identity information of the authenticated user.</summary>
  public UserDto? User { get; init; }

  /// <summary>
  /// List of role names assigned to this user (e.g. "Customer", "Admin", "Manager").
  /// Clients may use this list to conditionally show or hide UI features.
  /// </summary>
  public List<string> Roles { get; init; } = new();
}

public class GetCurrentUserEndpoint(UserManager<ApplicationUser> userManager)
  : Ep.NoReq.Res<GetCurrentUserResponse>
{
  public override void Configure()
  {
    Get("/api/auth/me");
    DontAutoTag();
    Description(b => b.WithTags("Authentication"));
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    var user = await userManager.FindByIdAsync(userId.ToString());
    if (user == null || !user.IsActive)
    {
      await SendUnauthorizedAsync(ct);
      return;
    }

    var roles = await userManager.GetRolesAsync(user);

    await SendOkAsync(new GetCurrentUserResponse
    {
      Success = true,
      User = new UserDto
      {
        Id = user.Id,
        Username = user.UserName,
        Email = user.Email,
        FullName = user.FullName
      },
      Roles = roles.ToList()
    }, ct);
  }
}
