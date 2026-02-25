using Microsoft.AspNetCore.Identity;

namespace Api.Infrastructure.Identity;

/// <summary>
/// Application user entity for authentication and authorization.
/// Handles ONLY auth concerns — domain data belongs to Customer/Staff aggregates in Core.
/// Linked to Customer via CustomerId (Guid?, no FK constraint across DBs).
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
  // === Profile Properties ===

  /// <summary>Full display name (required).</summary>
  public string FullName { get; set; } = string.Empty;

  /// <summary>Links to Staff aggregate (if this user is a staff member).</summary>
  public Guid? StaffId { get; set; }

  /// <summary>Links to Customer aggregate (if this user is a customer).</summary>
  public Guid? CustomerId { get; set; }

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
  /// All refresh tokens for this user. Use <see cref="RefreshToken.IsRevoked"/> to filter active ones.
  /// </summary>
  public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

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
