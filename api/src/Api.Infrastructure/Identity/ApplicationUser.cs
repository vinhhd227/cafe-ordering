using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Application user entity for authentication and authorization.
/// Handles ONLY auth concerns — domain data belongs to Customer aggregate in Core.
/// Linked to Customer via IdentityGuid (string, no FK constraint).
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
  // === Audit Properties ===

  public DateTime CreatedAt { get; set; }
  public string? CreatedBy { get; set; }
  public DateTime UpdatedAt { get; set; }
  public string? UpdatedBy { get; set; }

  // === Authentication Properties ===

  public bool IsActive { get; set; } = true;

  // === Navigation Properties ===

  public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

  /// <summary>
  /// All refresh tokens for this user (one per device/session).
  /// Use <see cref="UserRefreshToken.IsActive"/> to filter active ones.
  /// </summary>
  public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();

  // === Authentication Methods ===

  public void Deactivate()
  {
    IsActive = false;
    LockoutEnd = DateTimeOffset.MaxValue;
  }

  public void Activate()
  {
    IsActive = true;
    LockoutEnd = null;
  }
}
