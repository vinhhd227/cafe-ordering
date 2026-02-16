using Microsoft.AspNetCore.Identity;

namespace Api.Core.Entities.Identity;

/// <summary>
/// Application user entity for authentication and authorization.
/// Uses int as primary key (Identity Framework default).
/// Handles ONLY auth concerns - domain data belongs to Customer aggregate.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
  // === Audit Properties ===

  public DateTime CreatedAt { get; set; }
  public string? CreatedBy { get; set; }
  public DateTime UpdatedAt { get; set; }
  public string? UpdatedBy { get; set; }

  // Note: RowVersion removed - redundant with ConcurrencyStamp from IdentityUser

  // === Authentication Properties ===

  public bool IsActive { get; set; } = true;
  public string? RefreshToken { get; set; }
  public DateTime? RefreshTokenExpiryTime { get; set; }

  // === Navigation Properties ===

  public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();

  // Link to Customer aggregate (FK)
  public string? CustomerId { get; set; } // FK to Customer.Id (string)

  // === Authentication Methods ===

  /// <summary>
  /// Set refresh token for JWT authentication
  /// </summary>
  public void SetRefreshToken(string refreshToken, DateTime expiryTime)
  {
    RefreshToken = refreshToken;
    RefreshTokenExpiryTime = expiryTime;
  }

  /// <summary>
  /// Clear refresh token
  /// </summary>
  public void ClearRefreshToken()
  {
    RefreshToken = null;
    RefreshTokenExpiryTime = null;
  }

  /// <summary>
  /// Deactivate user account
  /// </summary>
  public void Deactivate()
  {
    IsActive = false;
    LockoutEnd = DateTimeOffset.MaxValue;
  }

  /// <summary>
  /// Activate user account
  /// </summary>
  public void Activate()
  {
    IsActive = true;
    LockoutEnd = null;
  }
}
