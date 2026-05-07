namespace Api.Infrastructure.Identity;

/// <summary>
/// Represents a refresh token issued to a user session.
/// Stored in Identity schema — one row per active session.
/// </summary>
public class RefreshToken
{
  public Guid Id { get; set; }

  /// <summary>FK to AspNetUsers (identity.Users)</summary>
  public Guid UserId { get; set; }

  /// <summary>SHA-256 hash of the raw token sent to the client. Never store plaintext.</summary>
  public string Token { get; set; } = string.Empty;

  public DateTime ExpiresAt { get; set; }

  public bool IsRevoked { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public bool RememberMe { get; set; }

  // === Navigation ===
  public ApplicationUser User { get; set; } = null!;

  // === Computed ===
  public bool IsActive => !IsRevoked && ExpiresAt > DateTime.UtcNow;

  // === Methods ===

  public void Revoke()
  {
    IsRevoked = true;
  }
}
